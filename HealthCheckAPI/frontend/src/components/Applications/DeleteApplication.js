import React, { useState } from "react";
import "./EditApplications.css"; // Βάλε το CSS που έδωσες εδώ

const appsInitial = [
    { id: 1, name: "App 1", type: "Mobile" },
    { id: 2, name: "App 2", type: "Web" },
    { id: 3, name: "App 3", type: "Desktop" },
];

const DeleteApplicationExample = () => {
    const [apps, setApps] = useState(appsInitial);
    const [appToDelete, setAppToDelete] = useState(null);
    const [showModal, setShowModal] = useState(false);
    const [message, setMessage] = useState("");

    const handleDeleteClick = (app) => {
        setAppToDelete(app);
        setShowModal(true);
    };

    const confirmDelete = () => {
        setApps((prev) => prev.filter((app) => app.id !== appToDelete.id));
        setMessage(`Η εφαρμογή "${appToDelete.name}" διαγράφηκε επιτυχώς!`);
        setShowModal(false);
        setAppToDelete(null);
    };

    const cancelDelete = () => {
        setShowModal(false);
        setAppToDelete(null);
    };

    return (
        <div className="edit-app-container">
            <h2>Οι Εφαρμογές μου</h2>

            {message && (
                <p className={`message ${message.includes("επιτυχ") ? "success" : "error"}`}>
                    {message}
                </p>
            )}

            <div className="app-list">
                {apps.map((app) => (
                    <div key={app.id} className="app-item" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "8px 0" }}>
                        <span>{app.name} ({app.type})</span>
                        <button
                            style={{ cursor: "pointer", background: "none", border: "none", fontSize: "20px", color: "#dc3545" }}
                            onClick={() => handleDeleteClick(app)}
                            aria-label={`Διαγραφή ${app.name}`}
                        >
                            ×
                        </button>
                    </div>
                ))}
            </div>

            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Επιβεβαίωση Διαγραφής</h3>
                        <p>Θες σίγουρα να διαγράψεις την εφαρμογή <strong>{appToDelete?.name}</strong>;</p>
                        <div className="modal-buttons">
                            <button className="modal-confirm" onClick={confirmDelete}>Ναι</button>
                            <button className="modal-cancel" onClick={cancelDelete}>Όχι</button>
                        </div>
                    </div>
                </div>
            )}

        </div>
    );
};

export default DeleteApplicationExample;
