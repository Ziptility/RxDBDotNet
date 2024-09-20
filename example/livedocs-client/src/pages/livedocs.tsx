import React from 'react';
import dynamic from 'next/dynamic';
import { PageContainer, StyledCircularProgress, CenteredBox } from '@/styles/StyledComponents';

const LiveDocsPageContent = dynamic(() => import('../components/LiveDocsPageContent'), {
  ssr: false,
  loading: () => (
    <CenteredBox sx={{ height: '50vh' }}>
      <StyledCircularProgress />
    </CenteredBox>
  ),
});

const LiveDocsPage: React.FC = () => {
  return (
    <PageContainer>
      <LiveDocsPageContent />
    </PageContainer>
  );
};

export default LiveDocsPage;
