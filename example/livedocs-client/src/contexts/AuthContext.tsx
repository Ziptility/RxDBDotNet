// example/livedocs-client/src/contexts/AuthContext.tsx

import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { ReactNode } from 'react';
import { API_CONFIG } from '@/config';
import type { User, Workspace } from '@/generated/graphql';
import { useDocuments } from '@/hooks/useDocuments';
import { getDatabase } from '@/lib/database';
import { updateReplicationToken } from '@/lib/replication';
import type { LiveDocsDatabase } from '@/types';
import { createTypedContext } from '@/utils/createTypedContext';
import { handleError, handleAsyncError } from '@/utils/errorHandling';

/**
 * Defines the shape of the authentication context.
 */
interface AuthContextType {
  currentUser: User | null;
  currentWorkspace: Workspace | null;
  jwtAccessToken: string | null;
  isLoggedIn: boolean;
  isInitialized: boolean;
  login: (userId: string, workspaceId: string) => Promise<void>;
  logout: () => Promise<void>;
  workspaces: Workspace[];
  users: User[];
}

const [useAuth, AuthProvider] = createTypedContext<AuthContextType>();

interface AuthProviderComponentProps {
  readonly children: ReactNode;
}

type AuthState = 'initializing' | 'ready' | 'error';

/**
 * AuthProviderComponent manages the authentication state and provides
 * authentication-related functionality to its children components.
 */
const AuthProviderComponent: React.FC<AuthProviderComponentProps> = ({ children }): JSX.Element => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);
  const [authState, setAuthState] = useState<AuthState>('initializing');

  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspace');
  const { documents: users, isLoading: isLoadingUsers } = useDocuments<User>('user');

  const loginRef = useRef<AuthContextType['login']>();
  const logoutRef = useRef<AuthContextType['logout']>();
  const initializationRef = useRef(false);

  const setToken = useCallback((token: string, storeInLocalStorage = true): void => {
    console.log('Setting token:', { token, storeInLocalStorage });
    setJwtAccessToken(token);
    if (storeInLocalStorage) {
      try {
        localStorage.setItem('jwtAccessToken', token);
      } catch (error) {
        handleError(error, 'AuthContext - setToken', { storeInLocalStorage });
      }
    }
  }, []);

  const updateReplicationTokenForDatabase = useCallback(async (db: LiveDocsDatabase, token: string): Promise<void> => {
    console.log('Updating replication token');
    await handleAsyncError(
      async () => {
        if (db.replicationStates) {
          await updateReplicationToken(db.replicationStates, token);
        } else {
          console.warn('Replication states are not initialized, skipping token update');
        }
      },
      'AuthContext - updateReplicationTokenForDatabase',
      { token }
    );
  }, []);

  const findUserAndWorkspace = useCallback(
    (userId: string, workspaceId: string): { user: User | undefined; workspace: Workspace | undefined } => {
      const user = users.find((u): boolean => u.id === userId);
      const workspace = workspaces.find((w): boolean => w.id === workspaceId);
      return { user, workspace };
    },
    [users, workspaces]
  );

  const storeAuthData = useCallback((userId: string, workspaceId: string, token: string): void => {
    try {
      localStorage.setItem('userId', userId);
      localStorage.setItem('workspaceId', workspaceId);
      localStorage.setItem('jwtAccessToken', token);
      console.log('Stored auth data', { userId, workspaceId, token });
    } catch (error) {
      console.error('Failed to store auth data in localStorage:', error);
    }
  }, []);

  const clearAuthData = useCallback((): void => {
    try {
      localStorage.removeItem('userId');
      localStorage.removeItem('workspaceId');
      localStorage.removeItem('jwtAccessToken');
      console.log('Cleared auth data');
    } catch (error) {
      console.error('Failed to clear auth data from localStorage:', error);
    }
  }, []);

  const setAuthStateAndData = useCallback(
    (user: User | null, workspace: Workspace | null, token: string): void => {
      setCurrentUser(user);
      setCurrentWorkspace(workspace);
      setToken(token);
      setIsLoggedIn(Boolean(user) && Boolean(workspace));
    },
    [setToken]
  );

  loginRef.current = useCallback(
    async (userId: string, workspaceId: string): Promise<void> => {
      await handleAsyncError(
        async () => {
          console.log('Logging in:', { userId, workspaceId });

          const db: LiveDocsDatabase = await getDatabase();
          const { user, workspace } = findUserAndWorkspace(userId, workspaceId);

          if (!user || !workspace) {
            throw new Error('Invalid user or workspace');
          }

          const token = user.jwtAccessToken ?? '';
          setAuthStateAndData(user, workspace, token);
          storeAuthData(userId, workspaceId, token);
          await updateReplicationTokenForDatabase(db, token);
        },
        'AuthContext - login',
        { userId, workspaceId }
      );
    },
    [findUserAndWorkspace, setAuthStateAndData, storeAuthData, updateReplicationTokenForDatabase]
  );

  logoutRef.current = useCallback(async (): Promise<void> => {
    console.log('Logging out');
    setAuthStateAndData(null, null, API_CONFIG.DEFAULT_JWT_TOKEN);
    clearAuthData();

    const db: LiveDocsDatabase = await getDatabase();
    await updateReplicationTokenForDatabase(db, API_CONFIG.DEFAULT_JWT_TOKEN);
  }, [setAuthStateAndData, clearAuthData, updateReplicationTokenForDatabase]);

  const initializeAuth = useCallback(async (): Promise<void> => {
    if (isInitialized || initializationRef.current) {
      console.log('Auth already initialized, skipping');
      return;
    }

    console.log('Initializing auth');
    initializationRef.current = true;

    let storedUserId: string | null = null;
    let storedWorkspaceId: string | null = null;
    let storedJwtToken: string | null = null;

    try {
      storedUserId = localStorage.getItem('userId');
      storedWorkspaceId = localStorage.getItem('workspaceId');
      storedJwtToken = localStorage.getItem('jwtAccessToken');
    } catch (error) {
      console.error('Failed to read auth data from localStorage:', error);
    }

    console.log('Stored auth data:', { storedUserId, storedWorkspaceId, storedJwtToken });

    try {
      const db: LiveDocsDatabase = await getDatabase();

      if (storedUserId !== null && storedWorkspaceId !== null && storedJwtToken !== null) {
        const { user, workspace } = findUserAndWorkspace(storedUserId, storedWorkspaceId);

        if (user && workspace) {
          setAuthStateAndData(user, workspace, storedJwtToken);
          await updateReplicationTokenForDatabase(db, storedJwtToken);
        } else {
          console.error('Invalid stored user or workspace');
          await logoutRef.current?.();
        }
      } else {
        console.log('No valid stored authentication data found. Using default token.');
        setAuthStateAndData(null, null, API_CONFIG.DEFAULT_JWT_TOKEN);
        // No need to update replication token here as it's already set up with the default token in database initialization
      }

      setAuthState('ready');
    } catch (error) {
      console.error('Error during auth initialization:', error);
      setAuthState('error');
    } finally {
      setIsInitialized(true);
    }
  }, [isInitialized, findUserAndWorkspace, setAuthStateAndData, updateReplicationTokenForDatabase]);

  useEffect(() => {
    if (!isInitialized && !initializationRef.current && !isLoadingWorkspaces && !isLoadingUsers) {
      void initializeAuth();
    }
  }, [initializeAuth, isInitialized, isLoadingWorkspaces, isLoadingUsers]);

  const contextValue: AuthContextType = {
    currentUser,
    currentWorkspace,
    jwtAccessToken,
    isLoggedIn,
    isInitialized,
    login: useCallback((userId: string, workspaceId: string): Promise<void> => {
      return loginRef.current ? loginRef.current(userId, workspaceId) : Promise.resolve();
    }, []),
    logout: useCallback(async (): Promise<void> => {
      if (logoutRef.current) {
        await logoutRef.current();
      }
    }, []),
    workspaces,
    users,
  };

  if (authState === 'initializing' || isLoadingWorkspaces || isLoadingUsers) {
    return <div>Loading...</div>;
  }

  if (authState === 'error') {
    return <div>Error initializing authentication. Please try refreshing the page.</div>;
  }

  return <AuthProvider value={contextValue}>{children}</AuthProvider>;
};

export { useAuth, AuthProviderComponent as AuthProvider };

/**
 * Best Practices and Notes for Maintainers:
 *
 * 1. State Management: This component uses React's useState for managing authentication state.
 *    Ensure that state updates are done correctly to avoid race conditions.
 *
 * 2. Memoization: useCallback is used extensively to memoize functions. This helps prevent
 *    unnecessary re-renders in child components that depend on these functions.
 *
 * 3. Side Effects: The useEffect hook is used for initialization. Be cautious when modifying
 *    the dependencies array to avoid infinite loops or missed updates.
 *
 * 4. Error Handling: Error states are managed and displayed. Consider implementing more
 *    robust error handling and recovery mechanisms for production use.
 *
 * 5. Local Storage: Authentication data is stored in localStorage for persistence across
 *    page reloads. Be aware of security implications and consider using more secure
 *    storage methods for sensitive data.
 *
 * 6. Performance: The component is designed to minimize re-renders. When making changes,
 *    consider the performance impact, especially for frequently changing state.
 *
 * 7. Replication: This component manages the authentication token used for replication.
 *    Ensure that token updates are properly propagated to the replication system.
 *
 * 8. Initialization: The auth state is initialized asynchronously. Ensure that your
 *    application can handle this async initialization process correctly.
 *
 * 9. Context Usage: This component provides an AuthContext. Ensure that consumers of this
 *    context handle potential undefined values correctly.
 *
 * 10. Logging: Extensive logging has been added to help diagnose issues. In a production
 *     environment, consider using a more sophisticated logging system and potentially
 *     reducing the verbosity of logs.
 *
 * 11. Type Safety: The code has been updated to be more type-safe. Always ensure that
 *     TypeScript's strict mode is enabled and that all types are properly defined.
 *
 * 12. Async Operations: Many operations in this component are asynchronous. Always use
 *     await with async functions and handle potential Promise rejections.
 *
 * 13. Dependency Management: Be cautious when adding or removing dependencies from useCallback
 *     and useEffect hooks. Missing dependencies can lead to stale closures, while unnecessary
 *     ones can cause excessive re-renders.
 *
 * 14. Null Checks: Always check for null or undefined values, especially when dealing with
 *     properties that might not be initialized, such as db.replicationStates.
 *
 * 15. Graceful Degradation: When a non-critical operation can't be performed (like updating
 *     replication token), log a warning and continue execution rather than throwing an error.
 *     This allows the application to function even if some features are temporarily unavailable.
 */
