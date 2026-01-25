import { APIRequestContext } from '@playwright/test';

const API_BASE_URL = 'http://localhost:8080';

export class ApiHelper {
  constructor(private apiClient: APIRequestContext) {}

  async register(email: string, password: string, displayName: string) {
    return this.apiClient.post(`${API_BASE_URL}/api/v1/auth/register`, {
      data: {
        email,
        password,
        displayName,
      },
    });
  }

  async login(email: string, password: string) {
    return this.apiClient.post(`${API_BASE_URL}/api/v1/auth/login`, {
      data: {
        email,
        password,
      },
    });
  }

  async getMe(token: string) {
    return this.apiClient.get(`${API_BASE_URL}/api/v1/users/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  async extendSession(token: string) {
    return this.apiClient.post(`${API_BASE_URL}/api/v1/auth/extend-session`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  async logout(token: string) {
    return this.apiClient.post(`${API_BASE_URL}/api/v1/auth/logout`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  }
}
