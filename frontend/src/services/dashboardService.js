import { codeRefineService } from './codeRefineService';

export const dashboardService = {
  getRepositories: codeRefineService.getRepositories,
  getBranches: codeRefineService.getBranches,
  getPullRequests: codeRefineService.getPullRequests,
  startAnalysis: codeRefineService.startAnalysis,
  getAnalysis: codeRefineService.getAnalysis,
  getFindings: codeRefineService.getFindings,
  getPatches: codeRefineService.getPatches,
  getVerification: codeRefineService.getVerification,
  approveAnalysis: codeRefineService.approveAnalysis,
  rejectAnalysis: codeRefineService.rejectAnalysis,
  createImprovementPullRequest: codeRefineService.createImprovementPullRequest,
};
