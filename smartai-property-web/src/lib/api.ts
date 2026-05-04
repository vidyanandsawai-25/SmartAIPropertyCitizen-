const API_BASE = process.env.NEXT_PUBLIC_API_URL || '/api';

export async function apiFetch(endpoint: string, options: RequestInit = {}) {
  const res = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'API Error' }));
    throw new Error(error.message || 'Something went wrong');
  }
  return res.json();
}

export const citizenApi = {
  getDiscounts: () => apiFetch('/citizen/discounts'),
  search: (searchInput: string) => apiFetch('/citizen/search', {
    method: 'POST',
    body: JSON.stringify({ searchInput }),
  }),
  sendOtp: (ownerId: number, propertyNo: string) => apiFetch('/citizen/send-otp', {
    method: 'POST',
    body: JSON.stringify({ ownerId, propertyNo }),
  }),
  verifyOtp: (sessionId: string, otp: string) => apiFetch('/citizen/verify-otp', {
    method: 'POST',
    body: JSON.stringify({ sessionId, otp }),
  }),
  chat: (message: string, sessionId: string, language: string) => apiFetch('/smartai/chat', {
    method: 'POST',
    body: JSON.stringify({ message, sessionId, language }),
  }),
};
