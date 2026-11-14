export interface RideRequest {
  departure: string;
  destination: string;
  startAt: string;
  meetPlace: string;
  rideTime: number;
  seats: number;
}

export interface Ride extends RideRequest {
  id: number;
  userId: number;
  availableSeats: number;
  isBookedByMe: boolean;
}
