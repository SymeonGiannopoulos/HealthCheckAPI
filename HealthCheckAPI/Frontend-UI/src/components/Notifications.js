import mqtt from 'mqtt';
import { useEffect } from 'react';
import { toast } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

function Notifications({ onNotification }) {
    useEffect(() => {
        const client = mqtt.connect('ws://localhost:9001');

        const handleMessage = (topic, message) => {
            const text = message.toString();

            toast.info(text, {
                position: "top-right",
                autoClose: 5000,
                hideProgressBar: false,
                closeOnClick: true,
                pauseOnHover: true,
                draggable: true,
                progress: undefined,
            });

            if (onNotification) {
                onNotification(text);
            }
        };

        client.on('connect', () => {
            console.log('Connected to MQTT broker via WebSocket');
            client.subscribe('notifications');
        });

        client.on('message', handleMessage);

        return () => {
            client.off('message', handleMessage);
            client.end();
        };
    }, [onNotification]);

    return null;
}

export default Notifications;
