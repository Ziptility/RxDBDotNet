// src/pages/_app.tsx
import React from 'react';
import { AppProps } from 'next/app';
import { EmotionCache } from '@emotion/cache';
import { ThemeProvider } from '@mui/material/styles';
import { CacheProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import theme from '../theme';
import createEmotionCache from '../createEmotionCache';
import Layout from '../components/Layout';
import { AuthProvider } from '@/contexts/AuthContext';
import ProtectedRoute from '@/components/ProtectedRoute';
import ErrorBoundary from '@/components/ErrorBoundary';

interface MyAppProps extends AppProps {
  emotionCache?: EmotionCache;
}

const clientSideEmotionCache = createEmotionCache();

function MyApp({ Component, pageProps, router, emotionCache = clientSideEmotionCache }: MyAppProps): JSX.Element {
  return (
    <CacheProvider value={emotionCache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <ErrorBoundary>
          <AuthProvider>
            <ToastContainer />
            {router.pathname === '/login' ? (
              <Component {...pageProps} />
            ) : (
              <ProtectedRoute>
                <Layout>
                  <Component {...pageProps} />
                </Layout>
              </ProtectedRoute>
            )}
          </AuthProvider>
        </ErrorBoundary>
      </ThemeProvider>
    </CacheProvider>
  );
}

export default MyApp;
