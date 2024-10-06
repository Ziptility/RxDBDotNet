/* eslint-disable react/no-multi-comp */
// example/livedocs-client/src/components/FormComponents.tsx
import React from 'react';
import { Alert, Box, Stack, Button } from '@mui/material';
import { PageTitle } from '@/styles/StyledComponents';

interface FormErrorProps {
  readonly error: string | null;
}

export const FormError: React.FC<FormErrorProps> = ({ error }) =>
  error !== null && error !== '' ? <Alert severity="error">{error}</Alert> : null;

interface FormLayoutProps {
  readonly title: string;
  readonly children: React.ReactNode;
  readonly onSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
}

export const FormLayout: React.FC<FormLayoutProps> = ({ title, children, onSubmit }) => (
  <Box component="form" onSubmit={onSubmit} noValidate autoComplete="off">
    {title !== '' ? (
      <PageTitle variant="h4" align="center" gutterBottom>
        {title}
      </PageTitle>
    ) : null}
    <Stack spacing={3}>{children}</Stack>
  </Box>
);

interface SubmitButtonProps {
  readonly label: string;
  readonly isSubmitting: boolean;
  readonly isValid: boolean;
  readonly onClick: () => void;
}

export const SubmitButton: React.FC<SubmitButtonProps> = ({
  label,
  isSubmitting,
  isValid,
  onClick,
}: SubmitButtonProps) => (
  <Button
    type="submit"
    variant="contained"
    color="primary"
    disabled={isSubmitting || !isValid}
    fullWidth
    onClick={onClick}
  >
    {isSubmitting ? 'Submitting...' : label}
  </Button>
);
