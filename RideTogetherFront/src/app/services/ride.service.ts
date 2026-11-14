import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Ride, RideRequest } from '../models/ride';
import {environment} from "../../environments/environment";

@Injectable({ providedIn: 'root' })
export class RideService {
  private readonly http = inject(HttpClient);
  private readonly options = { withCredentials: true };

  getAll() {
    return this.http.get<Ride[]>(`${environment.baseApiUrl}/Ride/all`, this.options);
  }

  search(query: string) {
    return this.http.post<Ride[]>(
      `${environment.baseApiUrl}/Ride/search`,
      { query },
      this.options,
    );
  }

  create(ride: RideRequest) {
    return this.http.post<Pick<Ride, "id">>(`${environment.baseApiUrl}/Ride/create`, ride, this.options);
  }

  update(id: number, ride: RideRequest) {
    return this.http.put<void>(`${environment.baseApiUrl}/Ride/update`, { ...ride, id }, this.options);
  }

  book(id: number) {
    return this.http.post<void>(`${environment.baseApiUrl}/Ride/${id}/book`, null, this.options);
  }

  delete(id: number) {
    return this.http.delete<void>(`${environment.baseApiUrl}/Ride/${id}`, this.options);
  }
}
