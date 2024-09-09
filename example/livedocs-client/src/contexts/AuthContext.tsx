import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { User, Workspace } from '@/lib/schemas';
import { getDatabase } from '@/lib/database';
import { RxDocument, RxCollection } from 'rxdb';

interface AuthContextType {
  currentUser: User | null;
  currentWorkspace: Workspace | null;
  jwtAccessToken: string | null;
  isLoggedIn: boolean;
  isInitialized: boolean;
  login: (userId: string, workspaceId: string) => Promise<void>;
  logout: () => void;
  workspaces: Workspace[];
  users: User[];
  fetchWorkspaces: () => Promise<void>;
  fetchUsers: (workspaceId: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }): JSX.Element => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [isInitialized, setIsInitialized] = useState<boolean>(false);
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [users, setUsers] = useState<User[]>([]);

  const login = useCallback(async (userId: string, workspaceId: string): Promise<void> => {
    console.log('AuthProvider: Attempting login with', { userId, workspaceId });
    try {
      const db = await getDatabase();
      const usersCollection: RxCollection<User> = db.users;
      const workspacesCollection: RxCollection<Workspace> = db.workspaces;

      const user: RxDocument<User> | null = await usersCollection
        .findOne({
          selector: {
            id: userId,
          },
        })
        .exec();

      const workspace: RxDocument<Workspace> | null = await workspacesCollection
        .findOne({
          selector: {
            id: workspaceId,
          },
        })
        .exec();

      if (user && workspace) {
        console.log('AuthProvider: Login successful, setting state');
        const userJson = user.toJSON();
        const workspaceJson = workspace.toJSON();

        localStorage.setItem('userId', userId);
        localStorage.setItem('workspaceId', workspaceId);
        localStorage.setItem('jwtAccessToken', userJson.jwtAccessToken ?? '');

        setCurrentUser(userJson);
        setCurrentWorkspace(workspaceJson);
        setJwtAccessToken(userJson.jwtAccessToken ?? null);
        setIsLoggedIn(true);
      } else {
        console.log('AuthProvider: Login failed, user or workspace not found');
        throw new Error('Invalid user or workspace');
      }
    } catch (error) {
      console.error('AuthProvider: Login error:', error);
      throw new Error('Login failed');
    }
  }, []); // Empty dependency array for login

  useEffect(() => {
    const initializeAuth = async (): Promise<void> => {
      console.log('AuthProvider: Starting initialization');
      const storedUserId = localStorage.getItem('userId');
      const storedWorkspaceId = localStorage.getItem('workspaceId');
      console.log('AuthProvider: Stored IDs:', { storedUserId, storedWorkspaceId });

      if (storedUserId && storedWorkspaceId) {
        console.log('AuthProvider: Attempting to log in with stored IDs');
        try {
          await login(storedUserId, storedWorkspaceId);
        } catch (error) {
          console.error('AuthProvider: Failed to log in with stored IDs', error);
          // Clear stored IDs if login fails
          localStorage.removeItem('userId');
          localStorage.removeItem('workspaceId');
        }
      } else {
        console.log('AuthProvider: No stored IDs, user is not logged in');
      }

      console.log('AuthProvider: Setting isInitialized to true');
      setIsInitialized(true);
    };

    void initializeAuth();
  }, [login]); // Add login to the dependency array

  const fetchWorkspaces = useCallback(async (): Promise<void> => {
    console.log('AuthProvider: Fetching workspaces');
    if (workspaces.length > 0) {
      console.log('AuthProvider: Workspaces already fetched, skipping');
      return;
    }
    try {
      const db = await getDatabase();
      const workspacesCollection: RxCollection<Workspace> = db.workspaces;
      const fetchedWorkspaces = await workspacesCollection.find().exec();
      console.log('AuthProvider: Fetched workspaces:', fetchedWorkspaces);
      setWorkspaces(fetchedWorkspaces);
    } catch (error) {
      console.error('AuthProvider: Error fetching workspaces:', error);
      throw new Error('Failed to fetch workspaces');
    }
  }, [workspaces]);

  const fetchUsers = useCallback(async (workspaceId: string): Promise<void> => {
    console.log('AuthProvider: Fetching users for workspace:', workspaceId);
    try {
      const db = await getDatabase();
      const usersCollection: RxCollection<User> = db.users;
      const fetchedUsers = await usersCollection
        .find({
          selector: {
            workspaceId: workspaceId,
          },
        })
        .exec();
      console.log('AuthProvider: Fetched users:', fetchedUsers);
      setUsers(fetchedUsers);
    } catch (error) {
      console.error('AuthProvider: Error fetching users:', error);
      throw new Error('Failed to fetch users');
    }
  }, []);

  const logout = useCallback((): void => {
    console.log('AuthProvider: Logging out');
    localStorage.removeItem('userId');
    localStorage.removeItem('workspaceId');
    localStorage.removeItem('jwtAccessToken');
    setCurrentUser(null);
    setCurrentWorkspace(null);
    setJwtAccessToken(null);
    setIsLoggedIn(false);
  }, []);

  console.log('AuthProvider: Current state', { isLoggedIn, isInitialized, workspacesCount: workspaces.length });

  const contextValue: AuthContextType = {
    currentUser,
    currentWorkspace,
    jwtAccessToken,
    isLoggedIn,
    isInitialized,
    login,
    logout,
    workspaces,
    users,
    fetchWorkspaces,
    fetchUsers,
  };

  return <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
