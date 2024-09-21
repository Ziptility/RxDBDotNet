// src/pages/users.tsx
import React from 'react';
import { motion } from 'framer-motion';
import dynamic from 'next/dynamic';
import { PageContainer, StyledCircularProgress, CenteredBox } from '@/styles/StyledComponents';
import { motionProps } from '@/utils/motionSystem';

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
      <motion.div {...motionProps['fadeIn']}>
        <UsersPageContent />
      </motion.div>
    </PageContainer>
  );
};

export default UsersPage;
