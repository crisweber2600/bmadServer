export class DatabaseHelper {
  private apiBaseUrl = 'http://localhost:8080';

  async createTestUser(email: string, password: string, role: string = 'User') {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/users`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        email,
        password,
        role,
        displayName: `Test User ${email}`,
      }),
    });
    return response.json();
  }

  async deleteTestUser(email: string) {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/users/${email}`, {
      method: 'DELETE',
    });
    return response.ok;
  }

  async cleanupAllTestData() {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/cleanup`, {
      method: 'POST',
    });
    return response.ok;
  }

  async getUserById(userId: string) {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/users/${userId}`, {
      method: 'GET',
    });
    return response.json();
  }

  async updateUserRole(email: string, role: string) {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/users/${email}/role`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ role }),
    });
    return response.json();
  }

  async getActiveSessions(email: string) {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/sessions/${email}`, {
      method: 'GET',
    });
    return response.json();
  }

  async verifyUserExists(email: string): Promise<boolean> {
    const response = await fetch(`${this.apiBaseUrl}/api/v1/test/users/${email}/exists`, {
      method: 'GET',
    });
    return response.ok;
  }
}
