import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './Auth.css';


const RegisterForm = () => {
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const navigate = useNavigate();

    const handleRegister = async (e) => {
        e.preventDefault();

        try {
            const res = await fetch('https://localhost:7057/api/Auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password, email })
            });

            if (!res.ok) {
                throw new Error('Registration failed');
            }

            navigate('/');
        } catch (err) {
            setError('Something went wrong.');
        }
    };

    return (
            <form className="auth-form" onSubmit={handleRegister}>
                <h2>Register</h2>
                <input type="text" placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} required />
                <input type="email" placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} required />
                <input type="password" placeholder="Password" value={password} onChange={e => setPassword(e.target.value)} required />
                <button type="submit">Register</button>
                {error && <p className="error">{error}</p>}
                <p>Already have an account? <a href="/">Login here</a></p>
            </form>
    );
};

export default RegisterForm;
