import React from 'react';
import dynamic from 'next/dynamic';
import { PageContainer, StyledCircularProgress, CenteredBox } from '@/styles/StyledComponents';

const WorkspacesPageContent = dynamic(() => import('../components/WorkspacesPageContent'), {
  ssr: false,
  loading: () => (
    <CenteredBox sx={{ height: '50vh' }}>
      <StyledCircularProgress />
    </CenteredBox>
  ),
});

const WorkspacesPage: React.FC = () => {
  return (
    <PageContainer>
      <WorkspacesPageContent />
    </PageContainer>
  );
};

export default WorkspacesPage;
