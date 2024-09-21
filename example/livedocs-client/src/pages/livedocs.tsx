// src\pages\livedocs.tsx
import React from 'react';
import { motion } from 'framer-motion';
import dynamic from 'next/dynamic';
import { PageContainer, StyledCircularProgress, CenteredBox } from '@/styles/StyledComponents';
import { motionProps } from '@/utils/motionSystem';

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
      <motion.div {...motionProps['fadeIn']}>
        <LiveDocsPageContent />
      </motion.div>
    </PageContainer>
  );
};

export default LiveDocsPage;
