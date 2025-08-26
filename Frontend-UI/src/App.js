import './App.css';
import { BrowserRouter as Router, Routes, Route, Navigate, NavLink, useLocation, useNavigate } from 'react-router-dom';
import React, { useState, useEffect } from 'react';

import AppManager from './components/AppManager/AppManager';
import AuditLogsView from './components/AuditLogs/AuditLogsView';
import Users from './components/Users/UserListView';
import LoginForm from './components/Auth/LoginForm';
import RegisterForm from './components/Auth/RegisterForm';
import ChatBotWidget from './components/ChatBot/ChatBotWidget';
import ErrorLogsView from './components/ErrorLogs/ErrorLogsView';
import Notifications from './components/Notifications';

import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';


const NavBar = ({ token, setToken }) => {
    const location = useLocation();
    const navigate = useNavigate();

    const handleLogout = () => {
        sessionStorage.removeItem('token');
        setToken(null);
        navigate('/');
    };

    if (!token || location.pathname === '/' || location.pathname === '/register') return null;

    return (
        <nav className="nav">
            <div className="nav-links">
                <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Αρχική
                </NavLink>
                <NavLink to="/audit-logs" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Audit Logs
                </NavLink>
                <NavLink to="/error-logs" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Error Logs
                </NavLink>
                <NavLink to="/users" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Users
                </NavLink>
            </div>
            <div className="nav-logout">
                <button onClick={handleLogout} title="Αποσύνδεση" className="logout-button">
                    <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                        <polyline points="16 17 21 12 16 7" />
                        <line x1="21" y1="12" x2="9" y2="12" />
                    </svg>
                </button>
            </div>
        </nav>
    );
};


function AppWrapper() {
    const [token, setToken] = useState(sessionStorage.getItem('token'));
    const [apps, setApps] = useState([]);

    useEffect(() => {
        const handleStorageChange = () => {
            setToken(sessionStorage.getItem('token'));
        };
        window.addEventListener('storage', handleStorageChange);
        return () => window.removeEventListener('storage', handleStorageChange);
    }, []);

    useEffect(() => {
        if (!token) return;

        fetch('https://localhost:7057/api/Apps', {
            headers: { 'Authorization': `Bearer ${token}` }
        })
            .then(res => res.json())
            .then(data => setApps(data))
            .catch(err => console.error(err));
    }, [token]);

    return (
        <div className="App">
            {/*  Global Toasts */}
            <ToastContainer position="top-right" autoClose={3000} hideProgressBar={false} closeOnClick pauseOnHover draggable />

            {/*  MQTT Notifications */}
            <Notifications />

            <h1>HealthCheck Dashboard</h1>
            <NavBar token={token} setToken={setToken} />

            <Routes>
                <Route
                    path="/"
                    element={token ? <Navigate to="/dashboard" /> :
                        <LoginForm setToken={token => {
                            sessionStorage.setItem('token', token);
                            setToken(token);
                        }} />}
                />
                <Route path="/register" element={<RegisterForm />} />
                <Route path="/dashboard" element={token ? <AppManager token={token} apps={apps} /> : <Navigate to="/" />} />
                <Route path="/audit-logs" element={token ? <AuditLogsView token={token} /> : <Navigate to="/" />} />
                <Route path="/users" element={token ? <Users token={token} /> : <Navigate to="/" />} />
                <Route path="/error-logs" element={token ? <ErrorLogsView token={token} apps={apps} /> : <Navigate to="/" />} />
            </Routes>

            {token && <ChatBotWidget />}
        </div>
    );
}


function App() {
    return (
        <Router>
            <AppWrapper />
        </Router>
    );
}

export default App;
