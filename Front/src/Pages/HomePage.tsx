import React, { useEffect, useState } from 'react';
import '../Styles/HomePageStyles.css';
import { EfficiencyReport } from '../Types/report';

const HomePage: React.FC = () => {
    const [report, setReport] = useState<EfficiencyReport | null>(null);
    const [error, setError] = useState<string | null>(null);

    const fetchReport = async () => {
        try {
            const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
const response = await fetch(`${apiUrl}/api/report`);
            if (!response.ok) throw new Error('Отчет не найден');
            const data = await response.json();
            setReport(data);
        } catch (err) {
            setError('Не удалось загрузить данные.');
        }
    };

    useEffect(() => {
        fetchReport();
        const interval = setInterval(fetchReport, 3000);
        return () => clearInterval(interval);
    },[]);

    if (error) return <div className="error-msg">{error}</div>;
    if (!report) return <div className="loading">Анализ кодовой базы...</div>;

    const sortedMethods = [...report.impactedMethods].sort((a, b) => a.propagationLevel - b.propagationLevel);

    return (
        <div className="dashboard">
            <header className="dashboard-header">
                <div>
                    <h1>Smart Test Selection System</h1>
                    <span className="subtitle">AI & Graph-based CI/CD Optimizer</span>
                </div>
                <div className="run-info">
                    <span className="pulse-dot"></span>
                    <span>Обновлено: {new Date(report.runDate).toLocaleTimeString()}</span>
                </div>
            </header>

            <div className="metrics-grid">
                <div className="metric-card">
                    <span className="label">Выполнение тестов</span>
                    <span className="value">{report.testsRunCount} <span className="dim">/ {report.totalTestsCount}</span></span>
                    <div className="progress-bg">
                        <div className="progress-fill" style={{ width: `${(report.testsRunCount / report.totalTestsCount) * 100}%` }}></div>
                    </div>
                </div>
                <div className="metric-card">
                    <span className="label">Время (секунды)</span>
                    <span className="value">{report.totalTestsTime.toFixed(2)}s</span>
                    <span className="trend down">Реальное время CI</span>
                </div>
                <div className="metric-card highlight">
                    <span className="label">Сэкономлено ресурсов</span>
                    <span className="value">{report.timeSaved.toFixed(1)}%</span>
                    <div className="progress-bg">
                        <div className="progress-fill success" style={{ width: `${report.timeSaved}%` }}></div>
                    </div>
                </div>
            </div>

            <section className="methods-section">
                <h2>Карта влияния (Impact Graph)</h2>
                <div className="table-header">
                    <span>Сигнатура метода</span>
                    <span>Уровень риска</span>
                </div>
                <ul className="method-list">
                    {sortedMethods.map((method, index) => {
                        const isDirect = method.propagationLevel === 0;
                        return (
                            <li key={index} className={`method-item ${!isDirect ? 'inferred' : 'direct'}`}>
                                <div className="method-info">
                                    <code>{method.fullName}</code>
                                </div>
                                <div className="method-badges">
                                    <span className={`badge ${isDirect ? 'danger' : 'warning'}`}>
                                        {isDirect ? 'DIRECT' : `LEVEL ${method.propagationLevel}`}
                                    </span>
                                </div>
                            </li>
                        );
                    })}
                </ul>
            </section>
        </div>
    );
};

export default HomePage;