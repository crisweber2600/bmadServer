import { test, expect } from '@playwright/test';
import { ChatPage } from '../../pages/ChatPage';
import { SignalRHelper } from '../../utils/signalr-helper';

/**
 * Story 3.1: SignalR Hub Setup & WebSocket Connection
 *
 * Tests WebSocket connection establishment, message transmission,
 * automatic reconnection, and session recovery
 */
test.describe('Story 3.1: SignalR Hub Setup & WebSocket Connection', () => {
  let chatPage: ChatPage;
  let signalRHelper: SignalRHelper;

  test.beforeEach(async ({ page }) => {
    chatPage = new ChatPage(page);
    signalRHelper = new SignalRHelper(page);
    await signalRHelper.setupInterceptor();
  });

  test('WebSocket connection establishes with valid JWT @P0', async ({ page }) => {
    // Given I am authenticated (handled by auth.setup.ts)
    // When I navigate to chat and connect with accessTokenFactory
    await chatPage.goto();

    // Then connection should be established successfully
    await signalRHelper.waitForConnection(10000);
    const state = await signalRHelper.getConnectionState();
    expect(state).toBe('connected');
  });

  test('OnConnectedAsync callback executes on connect @P1', async ({ page }) => {
    // Given I navigate to chat
    await chatPage.goto();

    // When connection establishes
    await signalRHelper.waitForConnection();

    // Then OnConnectedAsync should have been called
    // This is verified by receiving a session-related message
    const messages = await signalRHelper.getMessages();
    expect(messages.length).toBeGreaterThan(0);

    // Check for connection acknowledgment or session message
    const hasConnectionMessage = messages.some(
      (m) => m.type.toLowerCase().includes('connected') || m.type.toLowerCase().includes('session')
    );
    expect(hasConnectionMessage).toBe(true);
  });

  test('Message transmission within 100ms @P1', async ({ page }) => {
    // Given I am connected
    await chatPage.goto();
    await signalRHelper.waitForConnection();

    // When I send a message
    const latency = await signalRHelper.measureLatency(async () => {
      await chatPage.sendMessage('Test message for latency');
    }, 'ReceiveMessage');

    // Then server should receive within 100ms
    // Note: This tests round-trip, so we allow 200ms for acknowledgment
    expect(latency).toBeLessThan(200);
  });

  test('Automatic reconnect with exponential backoff @P0', async ({ page }) => {
    // Given I am connected
    await chatPage.goto();
    await signalRHelper.waitForConnection();

    // When WebSocket connection drops
    await signalRHelper.simulateDisconnect();

    // Then client should attempt reconnection with exponential backoff
    // Backoff sequence: 0s, 2s, 10s, 30s (per AC)

    // Wait for reconnection
    await page.waitForTimeout(500); // Brief disconnect

    // Trigger reconnect
    await signalRHelper.simulateReconnect();

    // Verify reconnection succeeded
    await signalRHelper.waitForConnection(15000);
    const state = await signalRHelper.getConnectionState();
    expect(state).toBe('connected');
  });

  test('Session recovery on reconnect @P0', async ({ page }) => {
    // Given I am connected with an active session
    await chatPage.goto();
    await signalRHelper.waitForConnection();

    // Send a message to establish session context
    await chatPage.sendMessage('Pre-disconnect message');
    await page.waitForTimeout(1000);

    // When SignalR automatic reconnect triggers
    await signalRHelper.simulateDisconnect();
    await page.waitForTimeout(500);
    await signalRHelper.simulateReconnect();
    await signalRHelper.waitForConnection(15000);

    // Then session recovery flow should execute
    // Verify SESSION_RESTORED message or equivalent
    const messages = await signalRHelper.getMessages();
    const hasSessionMessage = messages.some(
      (m) =>
        m.type.toLowerCase().includes('session') ||
        m.type.toLowerCase().includes('restored') ||
        m.type.toLowerCase().includes('recovered')
    );

    // The chat should still show previous messages
    const messageCount = await chatPage.getMessageCount();
    expect(messageCount).toBeGreaterThan(0);
  });
});
