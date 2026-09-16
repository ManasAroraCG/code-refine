import { api } from './api';

const ACTIVE_ANALYSIS_KEY = 'coderefine.activeAnalysis';

const rememberAnalysis = (analysis) => {
  if (analysis?.id) localStorage.setItem(ACTIVE_ANALYSIS_KEY, analysis.id);
  return analysis;
};

const readActiveAnalysisId = () => localStorage.getItem(ACTIVE_ANALYSIS_KEY);

export const codeRefineService = {
  getRepositories: async () => (await api.get('/repositories')).data,

  getBranches: async (repositoryId) =>
    (await api.get(`/repositories/${repositoryId}/branches`)).data,

  getPullRequests: async (repositoryId, state = 'open') =>
    (await api.get(`/repositories/${repositoryId}/pull-requests`, { params: { state } })).data,

  getPullRequest: async (repositoryId, pullRequestNumber) =>
    (await api.get(`/repositories/${repositoryId}/pull-requests/${pullRequestNumber}`)).data,

  getPullRequestFiles: async (repositoryId, pullRequestNumber) =>
    (await api.get(`/repositories/${repositoryId}/pull-requests/${pullRequestNumber}/files`)).data,

  startAnalysis: async ({ repositoryId, pullRequestNumber, branch } = {}) => {
    let selectedRepositoryId = repositoryId;
    let selectedGitHubRepoId;
    let selectedPullRequestNumber = pullRequestNumber;

    if (!selectedRepositoryId) {
      const repositories = await codeRefineService.getRepositories();
      const repository = repositories[0];
      selectedRepositoryId = repository?.id;
      selectedGitHubRepoId = repository?.gitHubRepoId;
    }

    if (!selectedRepositoryId) throw new Error('No repositories are available for analysis.');

    if (!selectedPullRequestNumber) {
      if (!selectedGitHubRepoId) {
        const repositories = await codeRefineService.getRepositories();
        selectedGitHubRepoId = repositories.find(({ id }) => id === selectedRepositoryId)?.gitHubRepoId;
      }
      const pullRequests = await codeRefineService.getPullRequests(selectedGitHubRepoId);
      selectedPullRequestNumber = pullRequests[0]?.number;
    }

    const response = await api.post('/analysis', {
      repositoryId: selectedRepositoryId,
      pullRequestNumber: selectedPullRequestNumber || null,
      branch: branch || null,
    });

    return rememberAnalysis(response.data);
  },

  getAnalysis: async (analysisId = readActiveAnalysisId()) =>
    (await api.get(`/analysis/${analysisId}`)).data,

  getFindings: async (analysisId = readActiveAnalysisId()) =>
    (await api.get(`/analysis/${analysisId}/findings`)).data,

  getPatches: async (analysisId = readActiveAnalysisId()) =>
    (await api.get(`/analysis/${analysisId}/patches`)).data,

  getVerification: async (analysisId = readActiveAnalysisId()) =>
    (await api.get(`/analysis/${analysisId}/verification`)).data,

  approveAnalysis: async (analysisId = readActiveAnalysisId(), payload = {}) =>
    (await api.post(`/analysis/${analysisId}/approve`, payload)).data,

  rejectAnalysis: async (analysisId = readActiveAnalysisId(), payload = {}) =>
    (await api.post(`/analysis/${analysisId}/reject`, payload)).data,

  createImprovementPullRequest: async (analysisId = readActiveAnalysisId(), payload = {}) =>
    (await api.post(`/analysis/${analysisId}/create-improvement-pr`, payload)).data,
};

export const getActiveAnalysisId = () => readActiveAnalysisId();
