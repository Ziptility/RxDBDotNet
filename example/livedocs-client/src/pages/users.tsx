import React from 'react';
import dynamic from 'next/dynamic';
import { PageContainer, StyledCircularProgress, CenteredBox } from '@/styles/StyledComponents';

const UsersPageContent = dynamic(() => import('../components/UsersPageContent'), {
  ssr: false,
  loading: () => (
    <CenteredBox sx={{ height: '50vh' }}>
      <StyledCircularProgress />
    </CenteredBox>
  ),
});

const UsersPage: React.FC = () => {
  return (
    <PageContainer>
      <UsersPageContent />
    </PageContainer>
  );
};

export default UsersPage;
