import React, { useEffect, useState } from 'react';
import { Card, Button, Input, Space, Spin, Collapse, Alert, Tag, Divider, Typography, Empty, Row, Col, Checkbox } from 'antd';
import { BugOutlined, PlayCircleOutlined, ClearOutlined, HeartOutlined, CopyOutlined } from '@ant-design/icons';
import './CopilotTest.css';

const { TextArea } = Input;
const { Text, Title } = Typography;

interface TestResponse {
    success: boolean;
    content?: string;
    requestedModel?: string;
    error?: string;
    errorType?: string;
    timedOut?: boolean;
    debugLog?: string[];
    eventLog?: string[];
    timestamp?: string;
}

interface BmadSettingsResponse {
    basePath: string;
    manifestPath: string;
    workflowManifestPath: string;
    enabledModules: string[];
    availableModules: string[];
    availableIdes: string[];
    manifestSourcePath?: string | null;
    reloadApplied?: boolean;
    requiresRestart?: boolean;
    message?: string | null;
}

interface BmadSettingsRequest {
    basePath?: string;
    manifestPath?: string;
    workflowManifestPath?: string;
    enabledModules?: string[];
}

export const CopilotTest: React.FC = () => {
    const [prompt, setPrompt] = useState('Tell me about the GitHub Copilot SDK.');
    const [systemMessage, setSystemMessage] = useState('You are a helpful assistant.');
    const [model, setModel] = useState('gpt-4');
    const [timeout, setTimeoutSeconds] = useState('30');
    const [isLoading, setIsLoading] = useState(false);
    const [testResponse, setTestResponse] = useState<TestResponse | null>(null);
    const [copiedIndex, setCopiedIndex] = useState<number | null>(null);
    const [settingsBasePath, setSettingsBasePath] = useState('');
    const [settingsManifestPath, setSettingsManifestPath] = useState('');
    const [settingsWorkflowManifestPath, setSettingsWorkflowManifestPath] = useState('');
    const [settingsModules, setSettingsModules] = useState<string[]>([]);
    const [availableModules, setAvailableModules] = useState<string[]>([]);
    const [availableIdes, setAvailableIdes] = useState<string[]>([]);
    const [settingsMessage, setSettingsMessage] = useState<string | null>(null);
    const [settingsError, setSettingsError] = useState<string | null>(null);
    const [settingsInfo, setSettingsInfo] = useState<BmadSettingsResponse | null>(null);
    const [isSettingsLoading, setIsSettingsLoading] = useState(false);
    const [isSettingsSaving, setIsSettingsSaving] = useState(false);

    const suggestedBasePath = 'D:\\bmad\\_bmad';

    const buildAuthHeaders = () => {
        const token = localStorage.getItem('jwt_token');
        const headers: Record<string, string> = {
            'Content-Type': 'application/json',
        };
        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }
        return headers;
    };

    const loadSettings = async () => {
        setIsSettingsLoading(true);
        setSettingsError(null);
        setSettingsMessage(null);

        try {
            const response = await fetch('/api/bmad/settings', {
                method: 'GET',
                headers: buildAuthHeaders(),
            });

            if (!response.ok) {
                const errorData = await response.json();
                setSettingsError(errorData.error || `HTTP ${response.status}`);
                return;
            }

            const data = (await response.json()) as BmadSettingsResponse;
            setSettingsInfo(data);
            setSettingsBasePath(data.basePath || '');
            setSettingsManifestPath(data.manifestPath || '');
            setSettingsWorkflowManifestPath(data.workflowManifestPath || '');
            setSettingsModules(data.enabledModules || []);
            setAvailableModules(data.availableModules || []);
            setAvailableIdes(data.availableIdes || []);
        } catch (err) {
            setSettingsError(err instanceof Error ? err.message : 'Failed to load BMAD settings');
        } finally {
            setIsSettingsLoading(false);
        }
    };

    const saveSettings = async () => {
        setIsSettingsSaving(true);
        setSettingsError(null);
        setSettingsMessage(null);

        const payload: BmadSettingsRequest = {
            basePath: settingsBasePath.trim() || undefined,
            manifestPath: settingsManifestPath.trim() || undefined,
            workflowManifestPath: settingsWorkflowManifestPath.trim() || undefined,
            enabledModules: settingsModules,
        };

        try {
            const response = await fetch('/api/bmad/settings', {
                method: 'PUT',
                headers: buildAuthHeaders(),
                body: JSON.stringify(payload),
            });

            if (!response.ok) {
                const errorData = await response.json();
                setSettingsError(errorData.error || `HTTP ${response.status}`);
                return;
            }

            const data = (await response.json()) as BmadSettingsResponse;
            setSettingsInfo(data);
            setSettingsMessage(data.message || 'BMAD settings updated.');
            setSettingsBasePath(data.basePath || '');
            setSettingsManifestPath(data.manifestPath || '');
            setSettingsWorkflowManifestPath(data.workflowManifestPath || '');
            setSettingsModules(data.enabledModules || []);
            setAvailableModules(data.availableModules || []);
            setAvailableIdes(data.availableIdes || []);
        } catch (err) {
            setSettingsError(err instanceof Error ? err.message : 'Failed to save BMAD settings');
        } finally {
            setIsSettingsSaving(false);
        }
    };

    useEffect(() => {
        void loadSettings();
    }, []);

    const handleTest = async () => {
        if (!prompt.trim()) {
            alert('Please enter a prompt');
            return;
        }

        setIsLoading(true);
        setTestResponse(null);

        try {
            const token = localStorage.getItem('jwt_token');
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
            };
            if (token) {
                headers.Authorization = `Bearer ${token}`;
            }

            const response = await fetch('/api/copilottest/test', {
                method: 'POST',
                headers,
                body: JSON.stringify({
                    prompt: prompt.trim(),
                    systemMessage: systemMessage.trim() || undefined,
                    model: model || undefined,
                    timeoutSeconds: parseInt(timeout) || undefined,
                }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                setTestResponse({
                    success: false,
                    error: errorData.error || `HTTP ${response.status}`,
                    errorType: errorData.errorType || 'HttpError',
                    debugLog: [],
                    eventLog: [],
                });
                return;
            }

            const data = await response.json() as TestResponse;
            setTestResponse(data);
        } catch (err) {
            setTestResponse({
                success: false,
                error: err instanceof Error ? err.message : 'Unknown error',
                errorType: 'NetworkError',
                debugLog: [],
                eventLog: [],
            });
        } finally {
            setIsLoading(false);
        }
    };

    const handleHealthCheck = async () => {
        setIsLoading(true);
        setTestResponse(null);

        try {
            const token = localStorage.getItem('jwt_token');
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
            };
            if (token) {
                headers.Authorization = `Bearer ${token}`;
            }

            const response = await fetch('/api/copilottest/health', {
                method: 'GET',
                headers,
            });

            const data = await response.json();
            setTestResponse({
                success: data.success ?? data.status === 'healthy',
                content: `Health Status: ${data.status}\nResponse Time: ${data.responseTime}ms`,
                debugLog: [
                    `Status: ${data.status}`,
                    `Response Time: ${data.responseTime}ms`,
                    `Timed Out: ${data.timedOut}`,
                ],
                eventLog: [],
            });
        } catch (err) {
            setTestResponse({
                success: false,
                error: err instanceof Error ? err.message : 'Unknown error',
                errorType: 'HealthCheckError',
                debugLog: [],
                eventLog: [],
            });
        } finally {
            setIsLoading(false);
        }
    };

    const handleClearAll = () => {
        setPrompt('');
        setSystemMessage('');
        setTestResponse(null);
    };

    const handleCopyToClipboard = (text: string, index: number) => {
        navigator.clipboard.writeText(text).then(() => {
            setCopiedIndex(index);
            window.setTimeout(() => setCopiedIndex(null), 2000);
        });
    };

    const debugItems = testResponse?.debugLog?.map((log, index) => ({
        key: `debug-${index}`,
        label: (
            <div className="copilot-test-log-item">
                <span>{log.substring(0, 80)}{log.length > 80 ? '...' : ''}</span>
            </div>
        ),
        extra: (
            <CopyOutlined
                onClick={(e) => {
                    e.stopPropagation();
                    handleCopyToClipboard(log, index);
                }}
                style={{ color: copiedIndex === index ? '#52c41a' : undefined }}
            />
        ),
        children: <Text code>{log}</Text>,
    })) || [];

    const eventItems = testResponse?.eventLog?.map((event, index) => ({
        key: `event-${index}`,
        label: (
            <div className="copilot-test-log-item">
                <span>{event.substring(0, 80)}{event.length > 80 ? '...' : ''}</span>
            </div>
        ),
        extra: (
            <CopyOutlined
                onClick={(e) => {
                    e.stopPropagation();
                    handleCopyToClipboard(event, index);
                }}
                style={{ color: copiedIndex === 100 + index ? '#52c41a' : undefined }}
            />
        ),
        children: <Text code>{event}</Text>,
    })) || [];

    return (
        <div className="copilot-test-container">
            <Card className="copilot-test-card">
                <div className="copilot-test-header">
                    <Title level={3}>
                        <BugOutlined style={{ marginRight: '8px' }} />
                        Copilot SDK Test Interface
                    </Title>
                    <Text type="secondary">Test raw Copilot integration with detailed debug output</Text>
                </div>

                <Divider />

                {/* BMAD Repository Settings */}
                <div className="copilot-test-section">
                    <Title level={4}>BMAD Repository Settings</Title>
                    <Text type="secondary">
                        Configure the BMAD repository directory and manifest paths used by Copilot workflows.
                    </Text>

                    {settingsError && (
                        <Alert
                            message="BMAD Settings Error"
                            description={settingsError}
                            type="error"
                            showIcon
                            style={{ marginTop: 16 }}
                        />
                    )}

                    {settingsMessage && (
                        <Alert
                            message="BMAD Settings"
                            description={settingsMessage}
                            type="success"
                            showIcon
                            style={{ marginTop: 16 }}
                        />
                    )}

                    {settingsInfo?.requiresRestart && (
                        <Alert
                            message="Restart Required"
                            description="BMAD settings were saved. Restart the API service to apply changes."
                            type="warning"
                            showIcon
                            style={{ marginTop: 16 }}
                        />
                    )}

                    <div className="copilot-test-form copilot-test-form-spaced">
                        <div className="form-group">
                            <label>BMAD Base Directory</label>
                            <Input
                                value={settingsBasePath}
                                onChange={(e) => setSettingsBasePath(e.target.value)}
                                placeholder={suggestedBasePath}
                                disabled={isSettingsLoading || isSettingsSaving}
                            />
                            <Space style={{ marginTop: 8 }}>
                                <Button
                                    onClick={() => setSettingsBasePath(suggestedBasePath)}
                                    disabled={isSettingsLoading || isSettingsSaving}
                                >
                                    Use D:\\bmad\\_bmad
                                </Button>
                                <Button
                                    onClick={loadSettings}
                                    loading={isSettingsLoading}
                                    disabled={isSettingsSaving}
                                >
                                    Reload
                                </Button>
                            </Space>
                        </div>

                        <Row gutter={16}>
                            <Col xs={24} sm={12}>
                                <div className="form-group">
                                    <label>Agent Manifest Path</label>
                                    <Input
                                        value={settingsManifestPath}
                                        onChange={(e) => setSettingsManifestPath(e.target.value)}
                                        placeholder="_config/agent-manifest.csv"
                                        disabled={isSettingsLoading || isSettingsSaving}
                                    />
                                </div>
                            </Col>
                            <Col xs={24} sm={12}>
                                <div className="form-group">
                                    <label>Workflow Manifest Path</label>
                                    <Input
                                        value={settingsWorkflowManifestPath}
                                        onChange={(e) => setSettingsWorkflowManifestPath(e.target.value)}
                                        placeholder="_config/workflow-manifest.csv"
                                        disabled={isSettingsLoading || isSettingsSaving}
                                    />
                                </div>
                            </Col>
                        </Row>

                        <div className="form-group">
                            <label>Enabled Modules</label>
                            {availableModules.length > 0 ? (
                                <Checkbox.Group
                                    options={availableModules.map((module) => ({ label: module, value: module }))}
                                    value={settingsModules}
                                    onChange={(values) => setSettingsModules(values as string[])}
                                    disabled={isSettingsLoading || isSettingsSaving}
                                />
                            ) : (
                                <Text type="secondary">No module list found. Update the base directory to load modules.</Text>
                            )}
                        </div>

                        {availableIdes.length > 0 && (
                            <div className="form-group">
                                <label>Detected IDE Targets</label>
                                <Space wrap>
                                    {availableIdes.map((ide) => (
                                        <Tag key={ide}>{ide}</Tag>
                                    ))}
                                </Space>
                            </div>
                        )}

                        {settingsInfo?.manifestSourcePath && (
                            <Text type="secondary">Manifest source: {settingsInfo.manifestSourcePath}</Text>
                        )}

                        <div className="form-actions">
                            <Space>
                                <Button
                                    type="primary"
                                    onClick={saveSettings}
                                    loading={isSettingsSaving}
                                    disabled={isSettingsLoading}
                                >
                                    Save BMAD Settings
                                </Button>
                                <Button
                                    onClick={loadSettings}
                                    disabled={isSettingsSaving}
                                >
                                    Reload
                                </Button>
                            </Space>
                        </div>
                    </div>
                </div>

                <Divider />

                {/* Input Section */}
                <div className="copilot-test-section">
                    <Title level={4}>Test Configuration</Title>

                    <div className="copilot-test-form">
                        <div className="form-group">
                            <label>Prompt *</label>
                            <TextArea
                                value={prompt}
                                onChange={(e) => setPrompt(e.target.value)}
                                placeholder="Enter your test prompt here..."
                                rows={4}
                                disabled={isLoading}
                                maxLength={2000}
                            />
                            <Text type="secondary">{prompt.length}/2000 characters</Text>
                        </div>

                        <div className="form-group">
                            <label>System Message (optional)</label>
                            <Input
                                value={systemMessage}
                                onChange={(e) => setSystemMessage(e.target.value)}
                                placeholder="Enter system message..."
                                disabled={isLoading}
                            />
                        </div>

                        <Row gutter={16}>
                            <Col xs={12} sm={12}>
                                <div className="form-group">
                                    <label>Model</label>
                                    <Input
                                        value={model}
                                        onChange={(e) => setModel(e.target.value)}
                                        placeholder="e.g., gpt-4"
                                        disabled={isLoading}
                                    />
                                </div>
                            </Col>
                            <Col xs={12} sm={12}>
                                <div className="form-group">
                                    <label>Timeout (seconds)</label>
                                    <Input
                                        type="number"
                                        value={timeout}
                                        onChange={(e) => setTimeoutSeconds(e.target.value)}
                                        placeholder="30"
                                        disabled={isLoading}
                                    />
                                </div>
                            </Col>
                        </Row>

                        <div className="form-actions">
                            <Space>
                                <Button
                                    type="primary"
                                    icon={<PlayCircleOutlined />}
                                    onClick={handleTest}
                                    loading={isLoading}
                                    disabled={!prompt.trim()}
                                    size="large"
                                >
                                    Test Copilot
                                </Button>
                                <Button
                                    icon={<HeartOutlined />}
                                    onClick={handleHealthCheck}
                                    loading={isLoading}
                                    size="large"
                                >
                                    Health Check
                                </Button>
                                <Button
                                    icon={<ClearOutlined />}
                                    onClick={handleClearAll}
                                    disabled={isLoading}
                                    size="large"
                                >
                                    Clear
                                </Button>
                            </Space>
                        </div>
                    </div>
                </div>

                <Divider />

                {/* Results Section */}
                {isLoading && (
                    <div className="copilot-test-loading">
                        <Spin size="large" tip="Testing Copilot SDK..." />
                    </div>
                )}

                {!isLoading && testResponse && (
                    <div className="copilot-test-results">
                        <Title level={4}>Test Results</Title>

                        {/* Status Alert */}
                        {testResponse.success ? (
                            <Alert
                                message="Success"
                                description="Copilot SDK responded successfully"
                                type="success"
                                showIcon
                                style={{ marginBottom: '16px' }}
                            />
                        ) : (
                            <Alert
                                message="Failed"
                                description={`${testResponse.error || 'Unknown error'} (${testResponse.errorType})`}
                                type="error"
                                showIcon
                                style={{ marginBottom: '16px' }}
                            />
                        )}

                        {testResponse.timedOut && (
                            <Alert
                                message="Timeout"
                                description="The request timed out"
                                type="warning"
                                showIcon
                                style={{ marginBottom: '16px' }}
                            />
                        )}

                        {/* Response Content */}
                        {testResponse.content && (
                            <div className="copilot-test-content">
                                <Title level={5}>Response Content</Title>
                                <div className="content-box">
                                    <Text>{testResponse.content}</Text>
                                </div>
                            </div>
                        )}

                        {/* Model Info */}
                        {testResponse.requestedModel && (
                            <div className="copilot-test-meta">
                                <Tag color="blue">Model: {testResponse.requestedModel}</Tag>
                                <Tag color="cyan">
                                    Timestamp: {new Date(testResponse.timestamp || '').toLocaleString()}
                                </Tag>
                            </div>
                        )}

                        {/* Debug Log */}
                        {debugItems.length > 0 && (
                            <div className="copilot-test-logs">
                                <Title level={5}>Debug Log ({debugItems.length} entries)</Title>
                                <Collapse items={debugItems} />
                            </div>
                        )}

                        {/* Event Log */}
                        {eventItems.length > 0 && (
                            <div className="copilot-test-logs">
                                <Title level={5}>Event Log ({eventItems.length} events)</Title>
                                <Collapse items={eventItems} />
                            </div>
                        )}

                        {debugItems.length === 0 && eventItems.length === 0 && !testResponse.content && (
                            <Empty description="No debug information available" />
                        )}
                    </div>
                )}

                {!isLoading && !testResponse && (
                    <Empty
                        description="Run a test to see results here"
                        style={{ marginTop: '32px' }}
                    />
                )}
            </Card>
        </div>
    );
};

export default CopilotTest;
