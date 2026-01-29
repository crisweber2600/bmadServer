import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Drawer,
  Input,
  Skeleton,
  Empty,
  Collapse,
  Tag,
  Typography,
  Space,
} from 'antd';
import {
  BookOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import { GlossaryTerm, PersonaType } from '../types/persona';
import defaultGlossary from '../data/glossary.json';
import './GlossaryPanel.css';

const { Text } = Typography;
const { Search } = Input;

export interface GlossaryPanelProps {
  open: boolean;
  onClose: () => void;
  terms?: GlossaryTerm[];
  fetchTerms?: () => Promise<GlossaryTerm[]>;
  currentPersona?: PersonaType;
}

export function GlossaryPanel({
  open,
  onClose,
  terms: providedTerms,
  fetchTerms,
  currentPersona,
}: GlossaryPanelProps): JSX.Element {
  // Only use default glossary if no terms and no fetchTerms provided
  const shouldUseDefaultGlossary = providedTerms === undefined && !fetchTerms;
  const [terms, setTerms] = useState<GlossaryTerm[]>(
    providedTerms ?? (shouldUseDefaultGlossary ? (defaultGlossary as GlossaryTerm[]) : [])
  );
  const [loading, setLoading] = useState<boolean>(!!fetchTerms && providedTerms === undefined);
  const [error, setError] = useState<string | null>(null);
  const [searchText, setSearchText] = useState<string>('');
  const [activeKey, setActiveKey] = useState<string[]>([]);

  const loadTerms = useCallback(async () => {
    if (!fetchTerms) return;

    setLoading(true);
    setError(null);

    try {
      const data = await fetchTerms();
      setTerms(data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load glossary terms';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [fetchTerms]);

  useEffect(() => {
    if (fetchTerms && open) {
      loadTerms();
    }
  }, [fetchTerms, open, loadTerms]);

  useEffect(() => {
    if (providedTerms) {
      setTerms(providedTerms);
    }
  }, [providedTerms]);

  const handleSearch = useCallback((value: string) => {
    setSearchText(value);
  }, []);

  const getCategoryColor = (category?: string): string => {
    switch (category) {
      case 'business':
        return 'blue';
      case 'technical':
        return 'green';
      case 'general':
      default:
        return 'default';
    }
  };

  const getPersonaCategoryOrder = useCallback((category?: string): number => {
    if (currentPersona === undefined || currentPersona === null) return 0;
    
    const persona = currentPersona as PersonaType;
    
    if (persona === PersonaType.Business) {
      if (category === 'business') return -2;
      if (category === 'general') return -1;
      return 0;
    }
    
    if (persona === PersonaType.Technical) {
      if (category === 'technical') return -2;
      if (category === 'general') return -1;
      return 0;
    }
    
    return 0;
  }, [currentPersona]);

  const filteredAndSortedTerms = useMemo(() => {
    let filtered = terms;

    // Filter by search text (case-insensitive)
    if (searchText.trim()) {
      const searchLower = searchText.toLowerCase();
      filtered = terms.filter((term) =>
        term.term.toLowerCase().includes(searchLower) ||
        term.definition.toLowerCase().includes(searchLower)
      );
    }

    // Sort alphabetically, then by persona category relevance
    return [...filtered].sort((a, b) => {
      const categoryOrderA = getPersonaCategoryOrder(a.category);
      const categoryOrderB = getPersonaCategoryOrder(b.category);
      
      if (categoryOrderA !== categoryOrderB) {
        return categoryOrderA - categoryOrderB;
      }
      
      return a.term.localeCompare(b.term);
    });
  }, [terms, searchText, getPersonaCategoryOrder]);

  const collapseItems = useMemo(() => {
    return filteredAndSortedTerms.map((term) => ({
      key: term.term,
      label: (
        <Space className="glossary-term-label">
          <Text strong>{term.term}</Text>
          {term.category && (
            <Tag color={getCategoryColor(term.category)} className="glossary-category-tag">
              {term.category}
            </Tag>
          )}
        </Space>
      ),
      children: (
        <div className="glossary-term-content">
          <Text>{term.definition}</Text>
          {term.relatedTerms && term.relatedTerms.length > 0 && (
            <div className="glossary-related-terms">
              <Text type="secondary">Related: </Text>
              <Space size={[0, 4]} wrap>
                {term.relatedTerms.map((related) => (
                  <Tag 
                    key={related} 
                    className="glossary-related-tag"
                    onClick={() => setSearchText(related)}
                    style={{ cursor: 'pointer' }}
                  >
                    {related}
                  </Tag>
                ))}
              </Space>
            </div>
          )}
        </div>
      ),
    }));
  }, [filteredAndSortedTerms]);

  const renderContent = () => {
    if (loading) {
      return <Skeleton active paragraph={{ rows: 6 }} data-testid="glossary-loading" />;
    }

    if (error) {
      return (
        <Empty
          description={
            <Space direction="vertical">
              <Text type="danger">{error}</Text>
              <button onClick={loadTerms} className="ant-btn ant-btn-link">
                Retry
              </button>
            </Space>
          }
        />
      );
    }

    if (!terms || terms.length === 0) {
      return (
        <Empty 
          description="No terms available" 
          data-testid="glossary-empty"
        />
      );
    }

    if (filteredAndSortedTerms.length === 0) {
      return (
        <Empty 
          description={`No terms matching "${searchText}"`} 
          data-testid="glossary-no-results"
        />
      );
    }

    return (
      <Collapse
        className="glossary-collapse"
        items={collapseItems}
        activeKey={activeKey}
        onChange={(keys) => setActiveKey(keys as string[])}
        data-testid="glossary-list"
      />
    );
  };

  return (
    <Drawer
      title={
        <Space>
          <BookOutlined />
          <span>Glossary</span>
        </Space>
      }
      placement="right"
      onClose={onClose}
      open={open}
      width={400}
      className="glossary-drawer"
      data-testid="glossary-panel"
    >
      <div className="glossary-content">
        <Search
          placeholder="Search terms..."
          prefix={<SearchOutlined />}
          onChange={(e) => handleSearch(e.target.value)}
          value={searchText}
          allowClear
          className="glossary-search"
          data-testid="glossary-search"
        />
        <div className="glossary-terms">
          {renderContent()}
        </div>
      </div>
    </Drawer>
  );
}

export default GlossaryPanel;
