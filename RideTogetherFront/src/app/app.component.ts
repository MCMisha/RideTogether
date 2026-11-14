import {HttpErrorResponse} from '@angular/common/http';
import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {NonNullableFormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {finalize, Observable, Subscription, timer} from 'rxjs';
import {Ride, RideRequest} from './models/ride';
import {AuthService} from './services/auth.service';
import {RideService} from './services/ride.service';
import {UserDto} from "./models/user-dto";

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly rideService = inject(RideService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private ridesRequest?: Subscription;

  readonly user = signal<UserDto | null>(null);
  readonly rides = signal<Ride[]>([]);
  readonly editingId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly signingIn = signal(false);
  readonly signingOut = signal(false);
  readonly saving = signal(false);
  readonly loadError = signal('');
  readonly loginError = signal('');
  readonly logoutError = signal('');
  readonly actionError = signal('');
  readonly bookingConflict = signal<{ rideId: number; message: string } | null>(null);

  readonly activeSearch = signal('');
  readonly searchForm = this.fb.group({
    query: ['', [Validators.maxLength(250)]],
  });
  readonly searchQuery = this.searchForm.controls.query;

  readonly loginForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  readonly form = this.fb.group({
    departure: ['', Validators.required],
    destination: ['', Validators.required],
    startAt: ['', Validators.required],
    meetPlace: ['', Validators.required],
    rideMinutes: [30, [Validators.required, Validators.min(1)]],
    seats: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
  });

  ngOnInit(): void {
    this.loadRides();
  }

  login(): void {
    if (this.user() || this.loginForm.invalid || this.signingIn() || this.saving()) return;

    const {username, password} = this.loginForm.getRawValue();
    this.loginError.set('');
    this.signingIn.set(true);
    this.auth.login(username.trim(), password)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.signingIn.set(false)))
      .subscribe({
        next: user => {
          this.user.set(user);
          this.loginForm.reset();
          this.cancelEdit();
          this.loadRides();
        },
        error: (error: HttpErrorResponse) => {
          this.loginForm.controls.password.reset();
          this.loginError.set(
            error.status === 400 || error.status === 401
              ? 'Incorrect username or password.'
              : 'Could not sign in.',
          );
        },
      });
  }

  logout(): void {
    if (!this.user() || this.signingOut() || this.saving()) return;

    this.logoutError.set('');
    this.signingOut.set(true);

    this.auth.logout()
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.signingOut.set(false)))
      .subscribe({
        next: () => this.resetSession(),
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.resetSession();
            return;
          }

          this.logoutError.set('Could not sign out. Please try again.');
        },
      });
  }

  private resetSession(): void {
    this.user.set(null);
    this.loginForm.reset();
    this.loginError.set('');
    this.logoutError.set('');
    this.bookingConflict.set(null);
    this.cancelEdit();
    this.loadRides();
  }

  searchRides(): void {
    const query = this.searchQuery.value.trim();
    if (!query || this.searchQuery.invalid || this.saving()) return;

    this.activeSearch.set(query);
    this.bookingConflict.set(null);
    this.loadRides();
  }

  clearSearch(): void {
    if (this.saving()) return;

    this.searchQuery.reset();
    if (!this.activeSearch()) return;

    this.activeSearch.set('');
    this.bookingConflict.set(null);
    this.loadRides();
  }

  loadRides(): void {
    this.ridesRequest?.unsubscribe();
    this.loading.set(true);
    this.loadError.set('');
    const query = this.activeSearch();
    const request = query ? this.rideService.search(query) : this.rideService.getAll();

    this.ridesRequest = request
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false)))
      .subscribe({
        next: rides => this.rides.set(rides),
        error: (error: HttpErrorResponse) => {
          const message = error.error?.message;
          if (query && error.status === 400 && typeof message === 'string' && message.trim()) {
            this.loadError.set(message);
            return;
          }

          this.loadError.set(query
            ? 'Search is unavailable. Please try again.'
            : 'Could not load rides.');
        },
      });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset();
    this.actionError.set('');
  }

  saveRide(): void {
    if (
      this.form.invalid ||
      !this.user() ||
      this.saving() ||
      this.signingIn() ||
      this.signingOut() ||
      this.loading()
    ) {
      return;
    }

    const {departure, destination, startAt, meetPlace, rideMinutes, seats} =
      this.form.getRawValue();

    const ride: RideRequest = {
      departure: departure.trim(),
      destination: destination.trim(),
      startAt: startAt.length === 16 ? `${startAt}:00` : startAt,
      meetPlace: meetPlace.trim(),
      rideTime: Math.round(rideMinutes * 60),
      seats,
    };

    const id = this.editingId();
    const request: Observable<unknown> = id === null
      ? this.rideService.create(ride)
      : this.rideService.update(id, ride);

    this.saving.set(true);
    this.actionError.set('');

    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: () => {
          this.cancelEdit();
          this.loadRides();
        },
        error: (error: HttpErrorResponse) => this.showActionError(error, id),
      });
  }

  editRide(ride: Ride): void {
    const user = this.user();

    if (
      !user ||
      user.id !== ride.userId ||
      this.saving() ||
      this.signingIn() ||
      this.signingOut()
    ) {
      return;
    }

    const [date, time] = ride.startAt.split('T');
    const [first, month, last] = date.split('-');
    const isoDate = first.length === 4 ? date : `${last}-${month}-${first}`;

    this.form.setValue({
      departure: ride.departure,
      destination: ride.destination,
      startAt: `${isoDate}T${time}`,
      meetPlace: ride.meetPlace,
      rideMinutes: ride.rideTime / 60,
      seats: ride.seats,
    });

    this.editingId.set(ride.id);
    this.actionError.set('');
  }

  bookRide(ride: Ride): void {
    const user = this.user();
    if (
      !user ||
      user.id === ride.userId ||
      ride.isBookedByMe ||
      ride.availableSeats <= 0 ||
      this.loading() ||
      this.saving() ||
      this.signingIn() ||
      this.signingOut()
    ) {
      return;
    }

    this.saving.set(true);
    this.actionError.set('');
    this.rideService.book(ride.id)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => this.loadRides(),
        error: (error: HttpErrorResponse) => {
          if (error.status === 409) {
            const conflict = {
              rideId: ride.id,
              message: error.error?.message ?? 'This ride could not be booked.'
            };

            this.bookingConflict.set(conflict);

            timer(5000)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe(() => {
                if (this.bookingConflict() === conflict) {
                  this.bookingConflict.set(null);
                }
              });
            this.loadRides();
            return;
          }
          this.showActionError(error, ride.id)
        },
      });
  }

  deleteRide(ride: Ride): void {
    if (!this.user() || this.user()?.id !== ride.userId || this.saving() || this.signingIn() || this.signingOut()) return;
    if (!window.confirm(`Delete the ride from ${ride.departure} to ${ride.destination}?`)) return;

    this.saving.set(true);
    this.actionError.set('');
    this.rideService.delete(ride.id)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          if (this.editingId() === ride.id) this.cancelEdit();
          this.loadRides();
        },
        error: (error: HttpErrorResponse) => this.showActionError(error, ride.id),
      });
  }

  private showActionError(error: HttpErrorResponse, rideId: number | null): void {
    switch (error.status) {
      case 401:
        this.user.set(null);
        this.cancelEdit();
        this.actionError.set('Please sign in again.');
        break;
      case 403:
        this.actionError.set('You do not have permission to perform this action.');
        break;
      case 404:
        if (rideId !== null && this.editingId() === rideId) {
          this.cancelEdit();
        }
        this.actionError.set('This ride is no longer available.');
        this.loadRides();
        break;
      case 409:
        this.actionError.set(error.error?.message ?? 'This ride has changed. Please try again.');
        this.loadRides();
        break;
      case 400:
        this.actionError.set('Check the ride details and try again.');
        break;
      default:
        this.actionError.set('Something went wrong. Please try again.');
    }
  }
}
