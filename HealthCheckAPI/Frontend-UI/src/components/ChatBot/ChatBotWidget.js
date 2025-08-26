import React, { useState } from 'react';
import './ChatBotWidget.css';

const ChatBotWidget = () => {
    const [open, setOpen] = useState(false);
    const [input, setInput] = useState('');
    const [messages, setMessages] = useState([]);

    const toggleChat = () => setOpen(!open);

    const handleSend = async () => {
        if (!input.trim()) return;

        const userMessage = { sender: 'user', text: input };
        setMessages(prev => [...prev, userMessage]);

        try {
            const token = localStorage.getItem('token');  
            const res = await fetch('https://localhost:7057/api/Chat/ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ question: input })
            });

            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }

            const data = await res.json(); 
            const botMessage = { sender: 'bot', text: data.answer };
            setMessages(prev => [...prev, botMessage]);
        } catch (err) {
            setMessages(prev => [...prev, { sender: 'bot', text: `Σφάλμα στην απόκριση: ${err.message}` }]);
        }

        setInput('');
    };


    return (
        <>
            {open && (
                <div className="chatbot-window">
                    <div className="chatbot-header">
                        ChatBot
                        <span style={{ cursor: 'pointer' }} onClick={toggleChat}>×</span>
                    </div>
                    <div className="chatbot-messages">
                        {messages.map((msg, i) => (
                            <div key={i}><strong>{msg.sender === 'user' ? 'Εσύ' : 'Bot'}:</strong> {msg.text}</div>
                        ))}
                    </div>
                    <div className="chatbot-input">
                        <input
                            type="text"
                            value={input}
                            onChange={e => setInput(e.target.value)}
                            onKeyDown={e => e.key === 'Enter' && handleSend()}
                            placeholder="Γράψε κάτι..."
                        />
                        <button onClick={handleSend}>Αποστολή</button>
                    </div>
                </div>
            )}
            <button className="chatbot-toggle" onClick={toggleChat}>💬</button>
        </>
    );
};

export default ChatBotWidget;
