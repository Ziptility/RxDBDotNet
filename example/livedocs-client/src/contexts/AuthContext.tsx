// src/contexts/AuthContext.tsx

import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { ReactNode } from 'react';
import { API_CONFIG } from '@/config';
import { useDocuments } from '@/hooks/useDocuments';
import { getDatabase } from '@/lib/database';
import { setupReplication, updateReplicationToken } from '@/lib/replication';
import type { User, Workspace } from '@/lib/schemas';
import type { LiveDocsDatabase } from '@/types';
import { createTypedContext } from '@/utils/createTypedContext';
import { handleAsyncError } from '@/utils/errorHandling';

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

  /**
   * Sets the JWT token and optionally stores it in localStorage.
   */
  const setToken = useCallback((token: string, storeInLocalStorage = true): void => {
    console.log('Setting token:', { token, storeInLocalStorage });
    setJwtAccessToken(token);
    if (storeInLocalStorage) {
      try {
        localStorage.setItem('jwtAccessToken', token);
      } catch (error) {
        console.error('Failed to store token in localStorage:', error);
      }
    }
  }, []);

  /**
   * Sets up or restarts replication with the given database and token.
   */
  const setupOrRestartReplication = useCallback(async (db: LiveDocsDatabase, token: string): Promise<void> => {
    console.log('Setting up or restarting replication');
    if (db.replicationStates) {
      await updateReplicationToken(db.replicationStates, token);
    } else {
      db.replicationStates = await setupReplication(db, token);
    }
  }, []);

  /**
   * Finds a user and workspace by their IDs.
   */
  const findUserAndWorkspace = useCallback(
    (userId: string, workspaceId: string): { user: User | undefined; workspace: Workspace | undefined } => {
      const user = users.find((u): boolean => u.id === userId);
      const workspace = workspaces.find((w): boolean => w.id === workspaceId);
      return { user, workspace };
    },
    [users, workspaces]
  );

  /**
   * Stores authentication data in localStorage.
   */
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

  /**
   * Clears authentication data from localStorage.
   */
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

  /**
   * Sets the authentication state and related data.
   */
  const setAuthStateAndData = useCallback(
    (user: User | null, workspace: Workspace | null, token: string): void => {
      setCurrentUser(user);
      setCurrentWorkspace(workspace);
      setToken(token);
      setIsLoggedIn(Boolean(user) && Boolean(workspace));
    },
    [setToken]
  );

  /**
   * Logs in a user with the given userId and workspaceId.
   */
  loginRef.current = useCallback(
    async (userId: string, workspaceId: string): Promise<void> => {
      await handleAsyncError(async () => {
        console.log('Logging in:', { userId, workspaceId });

        const db: LiveDocsDatabase = await getDatabase();
        const { user, workspace } = findUserAndWorkspace(userId, workspaceId);

        if (!user || !workspace) {
          throw new Error('Invalid user or workspace');
        }

        const token = user.jwtAccessToken ?? '';
        setAuthStateAndData(user, workspace, token);
        storeAuthData(userId, workspaceId, token);
        await setupOrRestartReplication(db, token);
      }, 'Login');
    },
    [findUserAndWorkspace, setAuthStateAndData, storeAuthData, setupOrRestartReplication]
  );

  /**
   * Logs out the current user.
   */
  logoutRef.current = useCallback(async (): Promise<void> => {
    console.log('Logging out');
    setAuthStateAndData(null, null, API_CONFIG.DEFAULT_JWT_TOKEN);
    clearAuthData();

    const db: LiveDocsDatabase = await getDatabase();
    await setupOrRestartReplication(db, API_CONFIG.DEFAULT_JWT_TOKEN);
  }, [setAuthStateAndData, clearAuthData, setupOrRestartReplication]);

  /**
   * Initializes the authentication state.
   */
  const initializeAuth = useCallback(async (): Promise<void> => {
    if (isInitialized) {
      console.log('Auth already initialized, skipping');
      return;
    }

    console.log('Initializing auth');
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
          await setupOrRestartReplication(db, storedJwtToken);
        } else {
          console.error('Invalid stored user or workspace');
          await logoutRef.current?.();
        }
      } else {
        console.log('No valid stored authentication data found. Using default token.');
        setAuthStateAndData(null, null, API_CONFIG.DEFAULT_JWT_TOKEN);
        await setupOrRestartReplication(db, API_CONFIG.DEFAULT_JWT_TOKEN);
      }

      setAuthState('ready');
    } catch (error) {
      console.error('Error during auth initialization:', error);
      setAuthState('error');
    } finally {
      setIsInitialized(true);
    }
  }, [findUserAndWorkspace, setAuthStateAndData, setupOrRestartReplication, isInitialized]);

  // Initialize authentication when the component mounts
  useEffect(() => {
    if (!isInitialized && !isLoadingWorkspaces && !isLoadingUsers) {
      void initializeAuth();
    }
  }, [initializeAuth, isInitialized, isLoadingWorkspaces, isLoadingUsers]);

  // Prepare the context value
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

  // Provide the authentication context to child components
  return <AuthProvider value={contextValue}>{children}</AuthProvider>;
};

export { useAuth, AuthProviderComponent as AuthProvider };
