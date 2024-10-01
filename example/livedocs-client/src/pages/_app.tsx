// src/pages/_app.tsx

import { useEffect } from 'react';
import { CacheProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import type { AppProps } from 'next/app';
import ErrorBoundary from '@/components/ErrorBoundary';
import ErrorHandler from '@/components/ErrorHandler';
import ProtectedRoute from '@/components/ProtectedRoute';
import { AuthProvider } from '@/contexts/AuthContext';
import { setupGlobalErrorListeners } from '@/utils/globalErrorListeners';
import Layout from '../components/Layout';
import { createEmotionCache } from '../createEmotionCache';
import { theme } from '../theme';
import type { EmotionCache } from '@emotion/cache';
import '@/styles/globals.css';

/**
 * Extended AppProps interface to include emotionCache.
 */
interface MyAppProps extends AppProps {
  readonly emotionCache: EmotionCache;
}

/**
 * Create a client-side cache, shared for the whole session of the user in the browser.
 */
const clientSideEmotionCache = createEmotionCache();

/**
 * MyApp Component
 *
 * This is the main application component in Next.js. It wraps the entire application and provides:
 * 1. Emotion's CacheProvider for CSS-in-JS styling
 * 2. Material-UI's ThemeProvider for consistent theming
 * 3. CssBaseline for consistent baseline styles
 * 4. ErrorBoundary for catching and handling React errors
 * 5. AuthProvider for authentication context
 * 6. ErrorHandler for centralized error handling UI
 * 7. Conditional rendering for protected routes
 *
 * @param Component - The active page component
 * @param router - The Next.js router object
 * @param pageProps - The initial props for the page
 * @param emotionCache - The Emotion cache for styling (optional, defaults to clientSideEmotionCache)
 *
 * Usage:
 * This file is automatically used by Next.js to initialize pages.
 * You don't need to import or use this component directly in your code.
 */
const MyApp = ({ Component, router, pageProps, emotionCache = clientSideEmotionCache }: MyAppProps): JSX.Element => {
  useEffect(() => {
    setupGlobalErrorListeners();
  }, []);

  return (
    <CacheProvider value={emotionCache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <ErrorBoundary>
          <AuthProvider>
            <ErrorHandler />
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
};

export default MyApp;
