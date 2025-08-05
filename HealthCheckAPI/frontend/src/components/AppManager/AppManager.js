import React, { useEffect, useState } from 'react';
import AppList from '../AppList/AppList';
import AddApplication from '../Applications/AddApplication';
import EditApplication from '../Applications/EditApplication';
import DeleteApplication from '../Applications/DeleteApplication'; 
import Modal from '../Shared/Modal'; 



const AppManager = () => {
    const [apps, setApps] = useState([]);
    const [selectedApp, setSelectedApp] = useState(null);
    const [showAddForm, setShowAddForm] = useState(false);
    const token = localStorage.getItem("token");

    useEffect(() => {
        if (!token) return;

        fetch('https://localhost:7057/api/Application', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        })
            .then(res => res.json())
            .then(data => setApps(data))
            .catch(err => console.error('Failed to load applications', err));
    }, [token]);

    const handleAddAppClick = () => {
        setShowAddForm(true);
    };

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
                headers: {
                    'Authorization': `Bearer ${token}`
                }
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

    return (
        <div className="app-manager-container">

            <AppList
                apps={apps}
                token={token}
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
        </div>
    );
};

export default AppManager;

