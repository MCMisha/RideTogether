import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {UserDto} from "../models/user-dto";

@Injectable({providedIn: 'root'})
export class AuthService {
  private readonly http = inject(HttpClient);

  login(username: string, password: string) {
    return this.http.post<UserDto>(
      `${environment.baseApiUrl}/User/login`,
      {username, password},
      {withCredentials: true},
    );
  }

  logout() {
    return this.http.post<void>(`${environment.baseApiUrl}/User/logout`, null, {withCredentials: true});
  }
}
