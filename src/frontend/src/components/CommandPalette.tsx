import React, { useState, useEffect, useRef, useMemo } from 'react';
import { List, Typography } from 'antd';
import './CommandPalette.css';

const { Text } = Typography;

export interface Command {
  name: string;
  description: string;
  /** If true, only shown when isPartyMode is true */
  partyModeOnly?: boolean;
}

const COMMANDS: Command[] = [
  { name: '/help', description: 'Show available commands' },
  { name: '/status', description: 'Check agent status' },
  { name: '/pause', description: 'Pause agent execution' },
  { name: '/resume', description: 'Resume agent execution' },
  // Party mode exit commands
  { name: '/exit', description: 'Exit party mode', partyModeOnly: true },
  { name: '/goodbye', description: 'End conversation and exit', partyModeOnly: true },
];

export interface CommandPaletteProps {
  onSelect: (command: string) => void;
  onClose: () => void;
  position?: { top: number; left: number };
  filter?: string;
  /** Whether party mode is active - shows exit commands when true */
  isPartyMode?: boolean;
  /** Callback when a party mode exit command is selected */
  onExitPartyMode?: () => void;
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({
  onSelect,
  onClose,
  position = { top: 0, left: 0 },
  filter = '',
  isPartyMode = false,
  onExitPartyMode,
}) => {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const paletteRef = useRef<HTMLDivElement>(null);

  // Filter commands based on input and party mode
  const filteredCommands = useMemo(() => {
    return COMMANDS
      .filter((cmd) => !cmd.partyModeOnly || isPartyMode) // Exclude party mode commands if not in party mode
      .filter((cmd) => cmd.name.toLowerCase().includes(filter.toLowerCase()));
  }, [filter, isPartyMode]);

  // Handle command selection including party mode exit
  const handleSelect = (command: string) => {
    // Check if it's a party mode exit command
    if ((command === '/exit' || command === '/goodbye') && onExitPartyMode) {
      onExitPartyMode();
    }
    onSelect(command);
  };

  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          setSelectedIndex((prev) => (prev + 1) % filteredCommands.length);
          break;
        case 'ArrowUp':
          e.preventDefault();
          setSelectedIndex((prev) => (prev - 1 + filteredCommands.length) % filteredCommands.length);
          break;
        case 'Enter':
          e.preventDefault();
          if (filteredCommands[selectedIndex]) {
            handleSelect(filteredCommands[selectedIndex].name);
          }
          break;
        case 'Escape':
          e.preventDefault();
          onClose();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [selectedIndex, filteredCommands, onSelect, onClose]);

  // Reset selected index when filtered commands change
  useEffect(() => {
    setSelectedIndex(0);
  }, [filter]);

  // Handle click outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (paletteRef.current && !paletteRef.current.contains(e.target as Node)) {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  if (filteredCommands.length === 0) {
    return null;
  }

  return (
    <div
      ref={paletteRef}
      className="command-palette"
      style={{
        position: 'absolute',
        top: position.top,
        left: position.left,
      }}
      role="listbox"
      aria-label="Command palette"
    >
      <List
        dataSource={filteredCommands}
        renderItem={(command, index) => (
          <List.Item
            key={command.name}
            className={`command-item ${index === selectedIndex ? 'command-item-selected' : ''}`}
            onClick={() => handleSelect(command.name)}
            role="option"
            aria-selected={index === selectedIndex}
            tabIndex={-1}
          >
            <div className="command-content">
              <Text strong>{command.name}</Text>
              <Text type="secondary" className="command-description">
                {command.description}
              </Text>
            </div>
          </List.Item>
        )}
      />
    </div>
  );
};
