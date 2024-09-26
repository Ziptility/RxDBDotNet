// src/contexts/AuthContext.tsx

import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { ReactNode } from 'react';
import { API_CONFIG } from '@/config';
import type { User, Workspace } from '@/generated/graphql';
import { useDocuments } from '@/hooks/useDocuments';
import { getDatabase } from '@/lib/database';
import { updateReplicationToken } from '@/lib/replication';
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
 *
 * This component leverages several React hooks for state management and optimization:
 *
 * 1. useState:
 *    Used for managing component-specific state that, when changed, should trigger a re-render.
 *    Each state variable is independent and can be updated separately.
 *    Example: const [currentUser, setCurrentUser] = useState<User | null>(null);
 *
 * 2. useEffect:
 *    Used for performing side effects after render, such as data fetching or subscriptions.
 *    It runs after every render by default, but dependencies can be specified to control when it runs.
 *    Example: useEffect(() => { ... }, [dep1, dep2]);
 *
 * 3. useCallback:
 *    Used to memoize functions, providing a stable function reference across re-renders.
 *    This is particularly useful for optimizing performance when passing callbacks to child components.
 *    Example: const memoizedFn = useCallback(() => { ... }, [dep1, dep2]);
 *
 * 4. useRef:
 *    Used for holding mutable values that persist across re-renders without causing re-renders when changed.
 *    Useful for storing values like DOM elements, interval IDs, or previous state for comparison.
 *    Example: const myRef = useRef(initialValue);
 *
 * Understanding re-renders:
 * A re-render in React occurs when a component's state or props change, or when its parent component re-renders.
 * Re-renders ensure the UI stays in sync with the component's data, but excessive re-renders can impact performance.
 *
 * When to use useRef vs. useState:
 * - Use useState for values that should cause a re-render when changed and be reflected in the UI.
 * - Use useRef for values that can change but shouldn't cause a re-render, like:
 *   1. Storing DOM elements
 *   2. Keeping track of interval IDs
 *   3. Caching values for comparison between renders
 *   4. Mutable values in event handlers or effects
 *
 * Performance considerations:
 * - Memoize callbacks and values that are passed to child components to prevent unnecessary re-renders.
 * - Use useRef for values that don't need to trigger re-renders when changed.
 * - Optimize useEffect dependencies to prevent unnecessary effect runs.
 *
 * IMPORTANT: Modifying the usage of these hooks without understanding their purpose may lead to unexpected behavior,
 * performance issues, or bugs. Always consider the implications of changes on re-render behavior and component lifecycle.
 */
const AuthProviderComponent: React.FC<AuthProviderComponentProps> = ({ children }): JSX.Element => {
  // State declarations using useState
  // These will trigger re-renders when changed, updating the UI
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);
  const [authState, setAuthState] = useState<AuthState>('initializing');

  // Custom hook to fetch documents
  // This abstracts the data fetching logic and provides loading states
  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspace');
  const { documents: users, isLoading: isLoadingUsers } = useDocuments<User>('user');

  // Refs for mutable values that don't require re-renders
  // These can be updated without causing the component to re-render
  const loginRef = useRef<AuthContextType['login']>();
  const logoutRef = useRef<AuthContextType['logout']>();
  const initializationRef = useRef(false);

  /**
   * Sets the JWT token and optionally stores it in localStorage.
   * This function is memoized with an empty dependency array.
   * It will remain the same across re-renders of this component,
   * but will be recreated if the component is unmounted and remounted.
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
   * Updates the replication token for the database.
   * This function is memoized with an empty dependency array.
   * It will remain the same across re-renders of this component,
   * but will be recreated if the component is unmounted and remounted.
   */
  const updateReplicationTokenForDatabase = useCallback(async (db: LiveDocsDatabase, token: string): Promise<void> => {
    console.log('Updating replication token');
    if (db.replicationStates) {
      await updateReplicationToken(db.replicationStates, token);
    }
  }, []);

  /**
   * Finds a user and workspace by their IDs.
   * This function is memoized and depends on users and workspaces.
   * It will be re-created when users or workspaces change, ensuring it always uses the latest data.
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
   * This function is memoized with an empty dependency array.
   * It will remain the same across re-renders of this component,
   * but will be recreated if the component is unmounted and remounted.
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
   * This function is memoized with an empty dependency array.
   * It will remain the same across re-renders of this component,
   * but will be recreated if the component is unmounted and remounted.
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
   * This function is memoized and depends on setToken.
   * It will be re-created if setToken changes, which should be never in this implementation.
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
   * This function is memoized and updates when its dependencies change.
   * It's stored in a ref to avoid infinite loops in effect dependencies.
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
        await updateReplicationTokenForDatabase(db, token);
      }, 'Login');
    },
    [findUserAndWorkspace, setAuthStateAndData, storeAuthData, updateReplicationTokenForDatabase]
  );

  /**
   * Logs out the current user.
   * This function is memoized and updates when its dependencies change.
   * It's stored in a ref to avoid infinite loops in effect dependencies.
   */
  logoutRef.current = useCallback(async (): Promise<void> => {
    console.log('Logging out');
    setAuthStateAndData(null, null, API_CONFIG.DEFAULT_JWT_TOKEN);
    clearAuthData();

    const db: LiveDocsDatabase = await getDatabase();
    await updateReplicationTokenForDatabase(db, API_CONFIG.DEFAULT_JWT_TOKEN);
  }, [setAuthStateAndData, clearAuthData, updateReplicationTokenForDatabase]);

  /**
   * Initializes the authentication state.
   * This function is memoized and updates when its dependencies change.
   * The dependencies ensure it has access to the latest state and functions.
   */
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

  /**
   * Effect to initialize authentication when the component mounts.
   * This effect runs when its dependencies change, which includes the initialization function
   * and loading states. It ensures auth is initialized only once when all required data is loaded.
   */
  useEffect(() => {
    if (!isInitialized && !initializationRef.current && !isLoadingWorkspaces && !isLoadingUsers) {
      void initializeAuth();
    }
  }, [initializeAuth, isInitialized, isLoadingWorkspaces, isLoadingUsers]);

  // Prepare the context value
  // This object will be passed down to all child components that use the useAuth hook
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

  // Render loading state if still initializing or loading essential data
  if (authState === 'initializing' || isLoadingWorkspaces || isLoadingUsers) {
    return <div>Loading...</div>;
  }

  // Render error state if initialization failed
  if (authState === 'error') {
    return <div>Error initializing authentication. Please try refreshing the page.</div>;
  }

  // Provide the authentication context to child components
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
 */
