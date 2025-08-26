import axios from 'axios';

const axiosInstance = axios.create({
    baseURL: 'https://localhost:7057'
});

axiosInstance.interceptors.request.use(config => {
    const token = sessionStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    } else {
        delete config.headers.Authorization;
    }
    return config;
}, error => {
    return Promise.reject(error);
});

export default axiosInstance;
