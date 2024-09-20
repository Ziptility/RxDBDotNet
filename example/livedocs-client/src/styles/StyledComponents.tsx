import { Box, Paper, Typography, Alert, Button } from '@mui/material';
import { styled } from '@mui/material/styles';
import type { BoxProps, PaperProps, TypographyProps, AlertProps, ButtonProps } from '@mui/material';

export const PageContainer = styled(Box)<BoxProps>(({ theme }) => ({
  padding: theme.spacing(6),
  maxWidth: '1200px',
  margin: '0 auto',
  backgroundColor: theme.palette.background.default,
}));

export const ContentPaper = styled(Paper)<PaperProps>(({ theme }) => ({
  padding: theme.spacing(6),
  marginBottom: theme.spacing(6),
  backgroundColor: theme.palette.background.paper,
}));

export const PageTitle = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.text.primary,
  marginBottom: theme.spacing(4),
}));

export const SectionTitle = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.text.secondary,
  marginBottom: theme.spacing(3),
}));

export const FormContainer = styled(Box)<BoxProps>(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  gap: theme.spacing(4),
}));

export const ListContainer = styled(Box)<BoxProps>(({ theme }) => ({
  marginTop: theme.spacing(6),
}));

export const FlexBox = styled(Box)<BoxProps>({
  display: 'flex',
});

export const CenteredBox = styled(Box)<BoxProps>({
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
});

export const SpaceBetweenBox = styled(Box)<BoxProps>({
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
});

export const ErrorText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.error.main,
}));

export const SuccessText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.success.main,
}));

export const WarningText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.warning.main,
}));

export const InfoText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.info.main,
}));

export const StyledForm = styled('form')(({ theme }) => ({
  width: '100%',
  marginTop: theme.spacing(3),
}));

export const StyledAlert = styled(Alert)<AlertProps>(({ theme }) => ({
  marginBottom: theme.spacing(4),
}));

export const PrimaryButton = styled(Button)<ButtonProps>(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  padding: theme.spacing(2, 4),
  '&:hover': {
    backgroundColor: theme.palette.primary.main,
  },
  '&:disabled': {
    backgroundColor: theme.palette.action.disabledBackground,
    color: theme.palette.action.disabled,
  },
}));

export const SecondaryButton = styled(Button)<ButtonProps>(({ theme }) => ({
  backgroundColor: theme.palette.secondary.main,
  color: theme.palette.secondary.contrastText,
  padding: theme.spacing(2, 4),
  '&:hover': {
    backgroundColor: theme.palette.secondary.main,
  },
  '&:disabled': {
    backgroundColor: theme.palette.action.disabledBackground,
    color: theme.palette.action.disabled,
  },
}));

// Re-export Material-UI components that don't need custom styling
export { TextField, CircularProgress, Select } from '@mui/material';
