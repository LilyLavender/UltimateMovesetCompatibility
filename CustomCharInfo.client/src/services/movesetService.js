import api from '@/services/api';

// Currently unused.

export const getMovesets = (params) => api.get('/movesets', { params });
export const getMoveset = (id) => api.get(`/movesets/${id}`);
export const createMoveset = (data) => api.post('/movesets', data);
export const updateMoveset = (id, data) => api.put(`/movesets/${id}`, data);
export const deleteMoveset = (id) => api.delete(`/movesets/${id}`);
