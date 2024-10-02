// src/utils/errorHandlerStateMachine.ts

import type { ErrorNotification } from '@/utils/errorHandling';

/**
 * Enum representing the possible states of the error handler
 */
export enum ErrorState {
  NO_ERRORS = 'NO_ERRORS',
  SINGLE_ERROR = 'SINGLE_ERROR',
  MULTIPLE_ERRORS_COLLAPSED = 'MULTIPLE_ERRORS_COLLAPSED',
  MULTIPLE_ERRORS_EXPANDED = 'MULTIPLE_ERRORS_EXPANDED',
  ERROR_DETAILS_VIEW = 'ERROR_DETAILS_VIEW',
}

/**
 * Interface representing the context for the error handler state machine
 */
export interface ErrorHandlerContext {
  errors: ErrorNotification[];
  expandedErrors: boolean;
  selectedErrorId: string | null;
}

/**
 * Type representing a transition in the state machine
 */
type Transition = (prevState: ErrorState, context: ErrorHandlerContext) => ErrorState;

/**
 * State machine for the error handler
 *
 * This state machine defines the transitions between different error states
 * and the actions that trigger these transitions. It ensures that the error
 * handling UI behaves consistently and predictably across various scenarios.
 */
export const errorHandlerStateMachine: Record<ErrorState, Record<string, Transition>> = {
  /**
   * NO_ERRORS State:
   * Initial state when there are no errors to display.
   */
  [ErrorState.NO_ERRORS]: {
    // Transition when an error occurs
    ERROR_OCCURRED: (_, context) =>
      // If it's the first error, go to SINGLE_ERROR state
      // Otherwise, go to MULTIPLE_ERRORS_COLLAPSED state
      context.errors.length === 1 ? ErrorState.SINGLE_ERROR : ErrorState.MULTIPLE_ERRORS_COLLAPSED,
  },

  /**
   * SINGLE_ERROR State:
   * State when there is exactly one error being displayed.
   */
  [ErrorState.SINGLE_ERROR]: {
    // Transition when an error is dismissed
    ERROR_DISMISSED: (_, context) =>
      // If all errors are dismissed, go back to NO_ERRORS state
      // Otherwise, remain in SINGLE_ERROR state
      context.errors.length === 0 ? ErrorState.NO_ERRORS : ErrorState.SINGLE_ERROR,

    // Transition when another error occurs
    ERROR_OCCURRED: (_, context) =>
      // If there's more than one error now, go to MULTIPLE_ERRORS_COLLAPSED state
      // Otherwise, remain in SINGLE_ERROR state
      context.errors.length > 1 ? ErrorState.MULTIPLE_ERRORS_COLLAPSED : ErrorState.SINGLE_ERROR,

    // Transition when user requests to view error details
    VIEW_DETAILS: () => ErrorState.ERROR_DETAILS_VIEW,
  },

  /**
   * MULTIPLE_ERRORS_COLLAPSED State:
   * State when there are multiple errors, but they are displayed in a collapsed view.
   */
  [ErrorState.MULTIPLE_ERRORS_COLLAPSED]: {
    // Transition when an error is resolved
    ERROR_DISMISSED: (_, context) => {
      if (context.errors.length === 0) return ErrorState.NO_ERRORS;
      if (context.errors.length === 1) return ErrorState.SINGLE_ERROR;
      return ErrorState.MULTIPLE_ERRORS_COLLAPSED;
    },

    // Transition when another error occurs
    ERROR_OCCURRED: () => ErrorState.MULTIPLE_ERRORS_COLLAPSED,

    // Transition when user expands the error list
    EXPAND_ERRORS: () => ErrorState.MULTIPLE_ERRORS_EXPANDED,

    // Transition when user requests to view error details
    VIEW_DETAILS: () => ErrorState.ERROR_DETAILS_VIEW,
  },

  /**
   * MULTIPLE_ERRORS_EXPANDED State:
   * State when there are multiple errors and they are displayed in an expanded view.
   */
  [ErrorState.MULTIPLE_ERRORS_EXPANDED]: {
    // Transition when an error is resolved
    ERROR_DISMISSED: (_, context) => {
      if (context.errors.length === 0) return ErrorState.NO_ERRORS;
      if (context.errors.length === 1) return ErrorState.SINGLE_ERROR;
      return ErrorState.MULTIPLE_ERRORS_EXPANDED;
    },

    // Transition when another error occurs
    ERROR_OCCURRED: () => ErrorState.MULTIPLE_ERRORS_EXPANDED,

    // Transition when user collapses the error list
    COLLAPSE_ERRORS: () => ErrorState.MULTIPLE_ERRORS_COLLAPSED,

    // Transition when user requests to view error details
    VIEW_DETAILS: () => ErrorState.ERROR_DETAILS_VIEW,
  },

  /**
   * ERROR_DETAILS_VIEW State:
   * State when the user is viewing detailed information about a specific error.
   */
  [ErrorState.ERROR_DETAILS_VIEW]: {
    // Transition when user closes the error details view
    CLOSE_DETAILS: (_, context) => {
      if (context.errors.length === 0) return ErrorState.NO_ERRORS;
      if (context.errors.length === 1) return ErrorState.SINGLE_ERROR;
      // Return to the appropriate multiple errors state based on whether errors were expanded
      return context.expandedErrors ? ErrorState.MULTIPLE_ERRORS_EXPANDED : ErrorState.MULTIPLE_ERRORS_COLLAPSED;
    },
  },
};
