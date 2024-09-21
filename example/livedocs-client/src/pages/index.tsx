// src/pages/index.tsx

import React from 'react';
import { Typography, Box } from '@mui/material';
import { motion } from 'framer-motion';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';

const Home: React.FC = () => {
  return (
    <Box sx={{ textAlign: 'center' }}>
      <motion.div {...staggeredChildren}>
        <motion.div {...motionProps['slideInFromBottom']}>
          <Typography variant="h2" component="h1" gutterBottom>
            Welcome to LiveDocs
          </Typography>
        </motion.div>
        <motion.div {...motionProps['slideInFromBottom']}>
          <Typography variant="h4" component="h2" gutterBottom>
            A real-time collaborative document management system
          </Typography>
        </motion.div>
        <motion.div {...motionProps['slideInFromBottom']}>
          <Typography variant="body1">
            Use the navigation links above to manage workspaces, users, and live documents.
          </Typography>
        </motion.div>
      </motion.div>
    </Box>
  );
};

export default Home;
