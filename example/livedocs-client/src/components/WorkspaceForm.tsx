import React, { useEffect } from 'react';
import { TextField, Button } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useForm, Controller } from 'react-hook-form';
import { FormLayout } from '@/components/FormComponents';
import type { Workspace } from '@/generated/graphql';

const StyledTextField = styled(TextField)(({ theme }) => ({
  '& .MuiInputBase-root': {
    backgroundColor: theme.palette.background.paper,
  },
}));

interface WorkspaceFormProps {
  readonly workspace: Workspace | null;
  readonly onSubmit: (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => void;
  readonly onCancel: () => void;
  readonly isInline: boolean;
}

const WorkspaceForm: React.FC<WorkspaceFormProps> = ({ workspace, onSubmit, onCancel, isInline = false }) => {
  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
    reset,
  } = useForm<Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>>({
    defaultValues: {
      name: workspace?.name ?? '',
    },
    mode: 'onChange',
  });

  useEffect(() => {
    reset({
      name: workspace?.name ?? '',
    });
  }, [workspace, reset]);

  const onSubmitForm = handleSubmit((data) => {
    onSubmit(data);
  });

  const formContent = (
    <>
      <Controller
        name="name"
        control={control}
        rules={{ required: 'Workspace name is required' }}
        render={({ field }) => (
          <StyledTextField
            {...field}
            label="Workspace Name"
            error={!!errors.name}
            helperText={errors.name?.message}
            fullWidth={!isInline}
            size={isInline ? 'small' : 'medium'}
            variant={isInline ? 'outlined' : 'filled'}
            slotProps={{
              input: {
                inputProps: {
                  maxLength: 30,
                },
              },
            }}
            sx={{
              width: 'auto',
              '& .MuiInputBase-input': {
                width: '30ch',
              },
            }}
          />
        )}
      />
      <Button
        type="submit"
        variant="contained"
        color="primary"
        disabled={isSubmitting || !isValid}
        size={isInline ? 'small' : 'medium'}
      >
        {workspace ? 'Update' : 'Create'}
      </Button>
      <Button onClick={onCancel} variant="outlined" color="secondary" size={isInline ? 'small' : 'medium'}>
        Cancel
      </Button>
    </>
  );

  return (
    <FormLayout
      title=""
      onSubmit={(e) => {
        void onSubmitForm(e);
      }}
    >
      {formContent}
    </FormLayout>
  );
};

export default WorkspaceForm;
