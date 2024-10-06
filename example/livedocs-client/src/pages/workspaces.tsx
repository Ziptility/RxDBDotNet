// example/livedocs-client/src/pages/workspaces.tsx
import React from 'react';
import { motion } from 'framer-motion';
import dynamic from 'next/dynamic';
import { PageContainer, StyledCircularProgress, CenteredBox } from '@/styles/StyledComponents';
import { motionProps } from '@/utils/motionSystem';

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
      <motion.div {...motionProps['fadeIn']}>
        <WorkspacesPageContent />
      </motion.div>
    </PageContainer>
  );
};

export default WorkspacesPage;
