export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresUtc: string;
  refreshToken: string;
  refreshTokenExpiresUtc: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}
