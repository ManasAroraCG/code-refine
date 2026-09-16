export const triggerDummyApi = async (actionName = 'Action') => {
  await new Promise((resolve) => setTimeout(resolve, 700));

  return {
    success: true,
    action: actionName,
    message: `${actionName} queued successfully. Replace this stub with the real API endpoint when ready.`,
  };
};

export const connectGitHub = () => triggerDummyApi('Connect GitHub');
export const startReview = () => triggerDummyApi('Start review');
export const approveChanges = () => triggerDummyApi('Approve changes');
export const reviewChanges = () => triggerDummyApi('Review changes');
export const requestDemo = () => triggerDummyApi('Request demo');