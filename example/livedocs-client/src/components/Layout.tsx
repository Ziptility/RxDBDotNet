// src/components/Layout.tsx
import React, { type ReactNode } from 'react';
import { Box } from '@mui/material';
import { motion } from 'framer-motion';
import { useRouter } from 'next/router';
import { PageContainer } from '@/styles/StyledComponents';
import { motionProps, AnimatePresence } from '@/utils/motionSystem';
import NavigationRail from './NavigationRail';

interface LayoutProps {
  readonly children: ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const router = useRouter();

  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        bgcolor: 'background.default',
      }}
    >
      <NavigationRail />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          display: 'flex',
          flexDirection: 'column',
          minHeight: '100vh',
          ml: '80px', // Width of the collapsed NavigationRail
          transition: (theme) =>
            theme.transitions.create('margin', {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.leavingScreen,
            }),
        }}
      >
        <PageContainer sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ flexGrow: 1 }}>
            <AnimatePresence mode="wait">
              <motion.div key={router.pathname} {...motionProps['fadeIn']}>
                {children}
              </motion.div>
            </AnimatePresence>
          </Box>
        </PageContainer>
      </Box>
    </Box>
  );
};

export default Layout;
