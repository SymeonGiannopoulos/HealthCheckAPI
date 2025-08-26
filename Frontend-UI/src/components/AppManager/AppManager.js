import React, { useState, useEffect } from 'react'; 
import AppList from '../AppList/AppList';
import AddApplication from '../Applications/AddApplication';
import EditApplication from '../Applications/EditApplication';
import Modal from '../Shared/Modal';
import Notifications from '../Notifications';
import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

const AppManager = () => {
    const [apps, setApps] = useState([]);
    const [selectedApp, setSelectedApp] = useState(null);
    const [showAddForm, setShowAddForm] = useState(false);
    const [refreshKey, setRefreshKey] = useState(0);
    const token = localStorage.getItem("token");

    const fetchApps = async () => {
        if (!token) return;
        try {
            const res = await fetch('https://localhost:7057/api/Application', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setApps(data);
            } else {
                console.error("Αποτυχία ανάκτησης εφαρμογών");
            }
        } catch (err) {
            console.error("Σφάλμα κατά την ανάκτηση εφαρμογών:", err);
        }
    };

    
    useEffect(() => {
        fetchApps();
    }, [token, refreshKey]);

    const handleAddAppClick = () => setShowAddForm(true);

    const handleAdd = (newApp) => {
        setApps(prev => [...prev, newApp]);
        setShowAddForm(false);
    };

    const handleEdit = (updatedApp) => {
        setApps(prev => prev.map(app => app.id === updatedApp.id ? updatedApp : app));
        setSelectedApp(null);
    };

    const handleDelete = async (id) => {
        const confirm = window.confirm("Είσαι σίγουρος ότι θες να διαγράψεις την εφαρμογή;");
        if (!confirm) return;

        try {
            const res = await fetch(`https://localhost:7057/api/Application/${id}`, {
                method: "DELETE",
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (res.ok) {
                setApps(prev => prev.filter(app => app.id !== id));
            } else {
                console.error("Αποτυχία διαγραφής");
            }
        } catch (err) {
            console.error("Σφάλμα κατά τη διαγραφή:", err);
        }
    };

    const handleNotification = () => setRefreshKey(prev => prev + 1);

    return (
        <div className="app-manager-container">
            {token && <Notifications onNotification={handleNotification} />}

            <AppList
                apps={apps}
                token={token}
                refreshKey={refreshKey}
                onEdit={app => setSelectedApp(app)}
                onDelete={handleDelete}
                onAddAppClick={handleAddAppClick}
            />

            <Modal isOpen={!!selectedApp} onClose={() => setSelectedApp(null)}>
                <EditApplication
                    app={selectedApp}
                    token={token}
                    onUpdate={handleEdit}
                    onCancel={() => setSelectedApp(null)}
                />
            </Modal>

            <Modal isOpen={showAddForm} onClose={() => setShowAddForm(false)}>
                <AddApplication
                    token={token}
                    onAdd={handleAdd}
                />
            </Modal>

            <ToastContainer
                position="top-right"
                autoClose={5000}
                hideProgressBar={false}
                newestOnTop={false}
                closeOnClick
                rtl={false}
                pauseOnFocusLoss
                draggable
                pauseOnHover
            />
        </div>
    );
};

export default AppManager;
