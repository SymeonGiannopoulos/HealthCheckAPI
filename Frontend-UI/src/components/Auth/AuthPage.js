import React, { useState } from 'react';
import LoginForm from './LoginForm';
import RegisterForm from './RegisterForm';

const AuthPage = () => {
    const [isLogin, setIsLogin] = useState(true);

    return (
        <div className="auth-page">
            {isLogin ? <LoginForm /> : <RegisterForm />}
            <div style={{ marginTop: '10px' }}>
                <button onClick={() => setIsLogin(!isLogin)} style={{ cursor: 'pointer' }}>
                    {isLogin ? "Δεν έχεις λογαριασμό; Εγγραφή" : "Έχεις ήδη λογαριασμό; Σύνδεση"}
                </button>
            </div>
        </div>
    );
};

export default AuthPage;
