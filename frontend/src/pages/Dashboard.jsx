import { useEffect, useState } from 'react';
import {
  ArrowDown,
  ArrowRight,
  Check,
  ChevronRight,
  CircleCheck,
  Code2,
  Container,
  GitBranch,
  Menu,
  Play,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  TrendingUp,
  X,
  Zap,
} from 'lucide-react';
import { dashboardService } from '../services/dashboardService';
import logo from '../assets/logos/Logo.png';

const beforeCode = `async function processUser(data) {
  if (data && data.email) {
    const valid = await validateEmail(data.email)
    if (valid) {
      const user = await db.users.find(data.email)
      if (user) return { user, ok: true }
    }
  }
  return { user: null, ok: false }
}`;

const afterCode = `async function processUser(data) {
  const email = data?.email
  if (!email || !(await validateEmail(email))) {
    return { user: null, ok: false }
  }
  const user = await db.users.find(email)
  return { user, ok: Boolean(user) }
}`;

const metrics = [
  ['Maintainability', '61', '91', '+49%'],
  ['Accuracy', '74', '93', '+26%'],
  ['Complexity', '18', '8', '-55%'],
  ['Readability', '65', '94', '+45%'],
  ['Security', '72', '89', '+24%'],
];

const features = [
  ['Pull Request Analysis', 'Get a complete quality report on every AI-assisted PR.', GitBranch],
  ['GitHub Integration', 'Connect your repositories in minutes. Reviews run automatically.', GitBranch],
  ['AI Refactoring Suggestions', 'Clear, actionable improvements your team can apply.', Sparkles],
  ['Code Quality Scoring', 'One reliable score across the dimensions that matter.', TrendingUp],
  ['Historical Comparison', 'See how code quality changes from branch to branch.', Zap],
  ['Team Reporting', 'Give engineering leaders a shared view of quality over time.', ShieldCheck],
];

function ProductMockup() {
  return (
    <div className="relative mx-auto w-full max-w-[680px] rounded-2xl border border-slate-200 bg-white p-3 shadow-[0_24px_80px_-32px_rgba(15,23,42,.28)] sm:p-6">
      <div className="flex items-center justify-between border-b border-slate-100 pb-4">
        <div className="flex items-center gap-2">
          <div className="flex size-7 items-center justify-center rounded-lg bg-blue-600 text-white">
            <Code2 className="size-4" />
          </div>
          <span className="text-xs font-semibold text-slate-800">PR #245 · auth-refresh</span>
        </div>
        <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700">
          Ready to merge
        </span>
      </div>

      <div className="grid gap-4 pt-5 sm:grid-cols-[.9fr_1.1fr]">
        <div className="rounded-xl bg-slate-950 p-4 text-white">
          <p className="text-[10px] uppercase tracking-[.18em] text-slate-500">Overall score</p>
          <div className="mt-3 flex items-end gap-2">
            <span className="text-5xl font-semibold tracking-tight">92</span>
            <span className="pb-2 text-xs text-emerald-400">+24%</span>
          </div>

          <div className="mt-5 flex flex-col gap-3">
            {[['Maintainability', '91'], ['Accuracy', '93'], ['Security', '89'], ['Readability', '94']].map(([label, value]) => (
              <div key={label}>
                <div className="flex justify-between text-[10px] text-slate-400">
                  <span>{label}</span>
                  <span className="text-white">{value}</span>
                </div>
                <div className="mt-1 h-1 rounded-full bg-white/10">
                  <div className="h-1 rounded-full bg-blue-400" style={{ width: `${value}%` }} />
                </div>
              </div>
            ))}
          </div>
        </div>

        <div>
          <div className="flex items-center justify-between">
            <p className="text-xs font-semibold text-slate-800">Before vs after</p>
            <span className="text-[10px] text-slate-400">5 dimensions</span>
          </div>

          <div className="mt-3 flex flex-col gap-3">
            {metrics.slice(0, 4).map(([label, before, after, delta]) => (
              <div key={label} className="rounded-lg border border-slate-100 p-3">
                <div className="flex items-center justify-between text-[11px]">
                  <span className="text-slate-500">{label}</span>
                  <span className="font-medium text-emerald-600">{delta}</span>
                </div>
                <div className="mt-2 flex items-center gap-2 text-sm">
                  <span className="text-slate-400">{before}</span>
                  <div className="h-px flex-1 bg-slate-200" />
                  <ChevronRight className="size-3 text-slate-300" />
                  <span className="font-semibold text-slate-900">{after}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function CodePanel({ after = false }) {
  const lines = (after ? afterCode : beforeCode).split('\n');

  return (
    <div className={`overflow-hidden rounded-2xl border ${after ? 'border-emerald-200 bg-emerald-50' : 'border-slate-700 bg-slate-900'}`}>
      <div className="flex items-center justify-between border-b border-inherit px-4 py-3">
        <span className={`font-mono text-[11px] ${after ? 'text-emerald-800' : 'text-slate-300'}`}>
          user-service.ts
        </span>
        <span className={`rounded-full border px-2 py-1 text-[10px] ${after ? 'border-emerald-300 text-emerald-800' : 'border-slate-600 text-slate-200'}`}>
          {after ? 'Refined' : 'Original'}
        </span>
      </div>

      <pre
        className={`overflow-x-auto p-4 text-[11px] leading-6 ${after ? 'text-slate-800' : 'text-slate-100'}`}
        style={after ? {} : { minHeight: '271.924px' }}
      >
        <code>
          {lines.map((line, index) => (
            <div key={index}>
              <span className="mr-4 inline-block w-4 select-none text-right text-slate-300">{index + 1}</span>
              <span className={after && [1, 2, 5, 6].includes(index) ? 'text-emerald-700' : ''}>{line}</span>
            </div>
          ))}
        </code>
      </pre>
    </div>
  );
}

function WorkflowStep({ number, title, description, children }) {
  return (
    <article className="relative rounded-3xl border border-slate-200 bg-white p-5 shadow-[0_20px_60px_-40px_rgba(15,23,42,.35)] transition-transform duration-500 hover:-translate-y-1 sm:p-7">
      <div className="flex items-center gap-3">
        <span className="flex size-8 items-center justify-center rounded-full bg-blue-600 font-mono text-xs font-semibold text-white">
          {number}
        </span>
        <h3 className="text-lg font-semibold tracking-tight text-slate-950">{title}</h3>
      </div>
      <div className="mt-6">{children}</div>
      <p className="mt-6 text-sm leading-6 text-slate-600">{description}</p>
    </article>
  );
}

function HowItWorks() {
  return (
    <section id="how-it-works" className="border-y border-slate-100 bg-slate-50/70 px-5 py-28 lg:px-8">
      <div className="mx-auto max-w-7xl">
        <div className="mx-auto max-w-3xl text-center">
          <p className="text-sm font-semibold text-blue-600">How it works</p>
          <h2 className="mt-4 text-4xl font-semibold tracking-[-.04em] sm:text-6xl">
            From Pull Request to Production-Ready Code.
          </h2>
          <p className="mt-6 text-lg leading-8 text-slate-600">
            Code Refine closes the loop between code review and verified improvement. It analyzes your Pull Request,
            generates targeted fixes, verifies them in an isolated environment, and prepares the changes for developer
            approval.
          </p>
        </div>

        <div className="relative mx-auto mt-16 max-w-5xl">
          <div className="pointer-events-none absolute bottom-12 left-1/2 top-12 hidden w-px -translate-x-1/2 bg-gradient-to-b from-blue-200 via-blue-400 to-emerald-300 md:block" />
          <div className="relative flex flex-col gap-5">
            <WorkflowStep number="01" title="GitHub Pull Request" description="Code Refine starts with the Pull Request you want to review.">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 font-mono text-xs">
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-slate-900">PR #245 · auth-refactor</span>
                  <span className="text-emerald-600">Ready for analysis</span>
                </div>
                <div className="mt-4 flex flex-wrap gap-3 text-slate-500">
                  <span>Files changed: 8</span>
                  <span className="text-emerald-600">+342</span>
                  <span className="text-rose-500">-128</span>
                </div>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-blue-200 bg-white text-blue-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="02" title="Code Analysis" description="The agent examines changed files together with the surrounding code and project context.">
              <div className="rounded-2xl border border-blue-100 bg-blue-50/70 p-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-semibold text-slate-900">Analyzing your code</span>
                  <span className="font-mono text-xs text-blue-600">47 files analyzed</span>
                </div>
                <div className="mt-4 h-2 overflow-hidden rounded-full bg-blue-100">
                  <div className="h-full w-[72%] animate-pulse rounded-full bg-blue-600" />
                </div>
                <div className="mt-3 flex justify-between text-xs text-slate-500">
                  <span>12,438 lines scanned</span>
                  <span>72%</span>
                </div>
                <div className="mt-4 grid gap-2 font-mono text-[11px] text-slate-600 sm:grid-cols-2">
                  <span>src/auth/AuthService.ts</span>
                  <span>src/auth/AuthController.ts</span>
                  <span>src/middleware/auth.ts</span>
                  <span>src/utils/token.ts</span>
                </div>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-blue-200 bg-white text-blue-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="03" title="AI Review Agents" description="Specialized agents inspect different aspects of the code.">
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
                <div className="col-span-2 flex items-center justify-center rounded-2xl border border-blue-200 bg-blue-50 p-4 text-center text-sm font-semibold text-blue-700 sm:col-span-5">
                  Code Refine Agent <Sparkles className="ml-2 size-4" />
                </div>
                {['Code Review', 'Security', 'Bug Detection', 'Performance', 'Maintainability'].map((agent, index) => (
                  <div
                    key={agent}
                    className="rounded-xl border border-slate-200 bg-white p-3 text-center text-xs font-medium text-slate-600 transition-colors duration-700 hover:border-blue-300 hover:text-blue-700"
                    style={{ animationDelay: `${index * 120}ms` }}
                  >
                    {agent} Agent
                  </div>
                ))}
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-blue-200 bg-white text-blue-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="04" title="Prioritized Findings" description="Findings are structured and prioritized so your team can act on what matters most.">
              <div className="grid gap-2 sm:grid-cols-3">
                {[
                  ['HIGH', 'Missing authorization check', 'src/auth/AuthController.ts:84', 'rose'],
                  ['MEDIUM', 'Repeated database query', 'src/auth/AuthService.ts:121', 'amber'],
                  ['LOW', 'Unused helper function', 'src/utils/token.ts:42', 'slate'],
                ].map(([level, issue, file, tone]) => (
                  <div key={issue} className="rounded-xl border border-slate-200 p-3">
                    <span className={`text-[10px] font-bold ${tone === 'rose' ? 'text-rose-600' : tone === 'amber' ? 'text-amber-600' : 'text-slate-500'}`}>
                      {level}
                    </span>
                    <p className="mt-2 text-xs font-semibold text-slate-800">{issue}</p>
                    <p className="mt-2 font-mono text-[10px] text-slate-500">{file}</p>
                  </div>
                ))}
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-blue-200 bg-white text-blue-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="05" title="Targeted Fix" description="Code Refine generates focused changes based on the detected issue instead of rewriting unrelated code.">
              <div className="grid overflow-hidden rounded-2xl border border-slate-200 bg-slate-950 font-mono text-[11px] sm:grid-cols-2">
                <div className="border-b border-slate-800 p-4 sm:border-b-0 sm:border-r">
                  <p className="mb-3 text-slate-500">Before</p>
                  <pre className="overflow-x-auto text-slate-300">{`if (user) {\n  return getProfile(user.id)\n}`}</pre>
                </div>
                <div className="p-4">
                  <div className="mb-3 flex items-center justify-between">
                    <span className="text-emerald-400">After</span>
                    <span className="rounded bg-emerald-400/10 px-2 py-1 text-[9px] text-emerald-300">
                      AI-generated patch
                    </span>
                  </div>
                  <pre className="overflow-x-auto text-emerald-300">{`if (!user) {\n  throw new AuthError()\n}\nreturn getProfile(user.id)`}</pre>
                </div>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-blue-200 bg-white text-blue-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="06" title="Docker Sandbox" description="Every generated change is tested in an isolated temporary environment before it reaches your codebase.">
              <div className="flex flex-col gap-3 rounded-2xl border-2 border-dashed border-blue-200 bg-blue-50/60 p-4 sm:flex-row sm:items-center">
                <Container className="size-8 shrink-0 text-blue-600" />
                <div className="grid flex-1 grid-cols-2 gap-2 text-xs text-slate-600 sm:grid-cols-3">
                  {['Repository Snapshot', 'Generated Patch', 'Dependencies', 'Tests', 'Build', 'Security Checks'].map((item) => (
                    <span key={item} className="rounded-lg bg-white px-3 py-2 shadow-sm">{item}</span>
                  ))}
                </div>
                <span className="text-xs font-medium text-slate-400">Production Repository</span>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-emerald-200 bg-white text-emerald-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="07" title="Verification" description="A clean verification result gives developers confidence in every proposed change.">
              <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-4">
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-emerald-900">Verification passed</span>
                  <span className="text-sm font-semibold text-emerald-700">214 tests passed</span>
                </div>
                <div className="mt-4 grid gap-2 text-xs text-emerald-800 sm:grid-cols-3">
                  {['Unit Tests', 'Integration Tests', 'Type Check', 'Lint', 'Build', 'Security Scan'].map((item) => (
                    <span key={item}>
                      <Check className="mr-1 inline size-3" />
                      {item}
                    </span>
                  ))}
                </div>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-amber-200 bg-white text-amber-600 shadow-sm">
              <RefreshCw className="size-4" />
            </div>

            <WorkflowStep number="08" title="Repair Loop" description="Code Refine uses verification failures as feedback and iterates on the fix.">
              <div className="grid gap-3 sm:grid-cols-[1fr_auto_1fr] sm:items-center">
                <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                  <p className="font-semibold text-amber-900">Verification failed</p>
                  <p className="mt-1 text-xs text-amber-700">2 tests failed · Failure Context</p>
                </div>
                <RefreshCw className="mx-auto size-5 text-blue-600" />
                <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-4">
                  <p className="font-semibold text-emerald-900">Verification passed</p>
                  <p className="mt-1 text-xs text-emerald-700">Revised Patch · Sandbox · 214 tests passed</p>
                </div>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-blue-200 bg-white text-blue-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="09" title="Developer Approval" description="AI proposes. Developers decide.">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                  <div>
                    <p className="font-semibold text-slate-900">Code Refine Improvement</p>
                    <p className="mt-2 text-xs text-slate-500">3 files modified · 4 findings addressed · 214 tests passed</p>
                  </div>
                  <div className="flex gap-2">
                    <button className="rounded-full bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
                      Approve Changes
                    </button>
                    <button className="rounded-full border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">
                      Review Changes
                    </button>
                  </div>
                </div>
              </div>
            </WorkflowStep>

            <div className="z-10 mx-auto flex size-9 items-center justify-center rounded-full border border-emerald-200 bg-white text-emerald-600 shadow-sm">
              <ArrowDown className="size-4" />
            </div>

            <WorkflowStep number="10" title="Improvement Pull Request" description="The loop closes with a verified PR ready for your team to review.">
              <div className="rounded-2xl border border-emerald-200 bg-white p-4 shadow-sm">
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-slate-900">Code Refine Improvement #246</span>
                  <span className="text-xs font-medium text-emerald-600">Ready for Review</span>
                </div>
                <p className="mt-3 text-sm text-slate-700">Improve authentication service</p>
                <div className="mt-4 flex flex-wrap gap-3 text-xs text-slate-500">
                  <span>3 files changed</span>
                  <span>214 tests passed</span>
                  <span>4 findings resolved</span>
                </div>
              </div>
            </WorkflowStep>
          </div>
        </div>

        <div className="mx-auto mt-20 max-w-2xl text-center">
          <h3 className="text-3xl font-semibold tracking-tight sm:text-5xl">Code review, closed-loop.</h3>
          <p className="mt-4 text-lg text-slate-600">Analyze. Fix. Verify. Repair. Improve.</p>
          <button className="mt-8 inline-flex items-center gap-2 rounded-full bg-blue-600 px-7 py-3 text-sm font-medium text-white hover:bg-blue-700">
            Try Code Refine <ArrowRight />
          </button>
        </div>
      </div>
    </section>
  );
}

export default function Dashboard() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [repositories, setRepositories] = useState([]);
  const [apiStatus, setApiStatus] = useState('Loading repositories...');

  useEffect(() => {
    dashboardService.getRepositories()
      .then((data) => {
        setRepositories(data);
        setApiStatus(`${data.length} repositories connected`);
      })
      .catch((error) => {
        setApiStatus(error.response?.data?.detail || error.message || 'Unable to load repositories');
      });
  }, []);

  return (
    <main className="min-h-screen overflow-hidden bg-white text-slate-950">
      <header className="fixed inset-x-0 top-0 z-50 border-b border-slate-200/70 bg-white/85 backdrop-blur-xl">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-5 lg:px-8">
          <a href="#top" className="flex items-center gap-2 font-semibold tracking-tight">
            <img src={logo} alt="Code Refine" className="h-10 w-auto mix-blend-multiply" />
            Code Refine
          </a>

          <nav
            className={`${menuOpen ? 'absolute inset-x-4 top-14 flex' : 'hidden'} flex-col gap-2 rounded-xl border border-slate-200 bg-white p-3 shadow-xl md:static md:flex md:flex-row md:items-center md:gap-7 md:border-0 md:bg-transparent md:p-0 md:shadow-none`}
          >
            <a href="#features" className="px-2 py-2 text-sm text-slate-600 hover:text-slate-950">Features</a>
            <a href="#how-it-works" className="px-2 py-2 text-sm text-slate-600 hover:text-slate-950">How It Works</a>
            <a href="#examples" className="px-2 py-2 text-sm text-slate-600 hover:text-slate-950">Examples</a>
            <a href="#metrics" className="px-2 py-2 text-sm text-slate-600 hover:text-slate-950">Metrics</a>
            <a href="#pricing" className="px-2 py-2 text-sm text-slate-600 hover:text-slate-950">Pricing</a>
            <a href="https://github.com" className="px-2 py-2 text-sm text-slate-600 hover:text-slate-950">GitHub</a>
            <button className="rounded-full bg-slate-950 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800">
              Get Started <ArrowRight className="ml-1 inline size-4" />
            </button>
          </nav>

          <button
            type="button"
            className="md:hidden"
            onClick={() => setMenuOpen(!menuOpen)}
            aria-label="Toggle menu"
          >
            <Menu />
          </button>
        </div>
      </header>

      <section className="px-5 pb-2 pt-28 lg:px-8">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 rounded-2xl border border-blue-100 bg-blue-50/70 p-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[.16em] text-blue-600">Live API connection</p>
            <p className="mt-2 text-sm text-slate-700">{apiStatus}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            {repositories.slice(0, 3).map((repository) => (
              <span key={repository.gitHubRepoId || repository.githubRepoId} className="rounded-full bg-white px-3 py-1 text-xs font-medium text-slate-700 shadow-sm">
                {repository.fullName}
              </span>
            ))}
          </div>
        </div>
      </section>

      <section id="top" className="relative px-5 pb-24 pt-36 sm:pt-44 lg:px-8">
        <div className="pointer-events-none absolute left-1/2 top-0 -z-0 h-[620px] w-[900px] -translate-x-1/2 bg-[radial-gradient(ellipse_at_center,rgba(219,234,254,.75),transparent_68%)]" />
        <div className="relative z-10 mx-auto grid max-w-7xl items-center gap-16 lg:grid-cols-[.9fr_1.1fr]">
          <div>
            <span className="inline-flex rounded-full border border-blue-200 bg-blue-50 px-3 py-1 text-blue-700">
              The quality layer for AI code
            </span>
            <h1 className="mt-7 max-w-3xl text-5xl font-semibold leading-[1.03] tracking-[-.055em] text-slate-950 sm:text-8xl">
              Turn AI-generated code into <span className="text-blue-600">production-ready</span> code.
            </h1>
            <p className="mt-7 max-w-xl text-xl leading-8 text-slate-600">
              Code Refine automatically analyzes AI-generated code from GitHub Pull Requests, measures code quality,
              and recommends improvements before merge.
            </p>

            <div className="mt-9 flex flex-col gap-3 sm:flex-row">
              <button className="rounded-full bg-blue-600 px-6 py-3 text-sm font-medium text-white hover:bg-blue-700">
                Get Started <ArrowRight className="ml-1 inline size-4" />
              </button>
              <button className="inline-flex items-center justify-center rounded-full border border-slate-300 px-5 py-3 text-sm font-medium text-slate-700 hover:bg-slate-100 sm:w-auto sm:px-6">
                <Play className="mr-2 size-4" /> View Example PR
              </button>
            </div>

            <div className="mt-8 flex items-center gap-5 text-xs text-slate-500">
              <span className="flex items-center gap-2">
                <CircleCheck className="size-4 text-emerald-500" /> No credit card required
              </span>
              <span className="flex items-center gap-2">
                <CircleCheck className="size-4 text-emerald-500" /> Works with GitHub
              </span>
            </div>
          </div>

          <ProductMockup />
        </div>
      </section>

      <section className="border-y border-slate-100 bg-slate-50/70 px-5 py-24 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <p className="text-center text-sm font-medium text-slate-500">AI writes code. But is it production-ready?</p>
          <h2 className="mx-auto mt-4 max-w-2xl text-center text-3xl font-semibold tracking-tight sm:text-5xl">
            Move fast without moving quality backward.
          </h2>

          <div className="mt-14 grid gap-4 md:grid-cols-3">
            {[
              ['Code bloat', 'Generated code is often verbose and hard to maintain.'],
              ['Hidden complexity', 'Functions grow complex and difficult to review.'],
              ['Quality risk', 'Small issues compound into technical debt.'],
            ].map(([title, text]) => (
              <div key={title} className="rounded-2xl border border-slate-200 bg-white p-7">
                <div className="flex size-10 items-center justify-center rounded-xl bg-slate-100 text-slate-500">
                  <X className="size-4" />
                </div>
                <h3 className="mt-8 text-lg font-semibold">{title}</h3>
                <p className="mt-3 leading-7 text-slate-600">{text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="px-5 py-24 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div className="max-w-2xl">
            <p className="text-sm font-semibold text-blue-600">How it works</p>
            <h2 className="mt-4 text-3xl font-semibold tracking-tight sm:text-5xl">
              A better review loop, built into GitHub.
            </h2>
          </div>

          <div className="mt-14 grid gap-5 md:grid-cols-3">
            {[
              ['01', 'Developer opens PR', 'Your existing workflow stays exactly the same.'],
              ['02', 'Code Refine analyzes', 'Every line is measured across five quality dimensions.'],
              ['03', 'Improve with confidence', 'Get precise suggestions before you merge.'],
            ].map(([number, title, text], index) => (
              <div key={number} className="relative border-t border-slate-200 pt-6">
                <span className="font-mono text-sm text-blue-600">{number}</span>
                <h3 className="mt-10 text-xl font-semibold">{title}</h3>
                <p className="mt-3 max-w-xs leading-7 text-slate-600">{text}</p>
                {index < 2 && <ArrowRight className="absolute right-3 top-7 hidden size-5 text-slate-300 md:block" />}
              </div>
            ))}
          </div>
        </div>
      </section>

      <HowItWorks />

      <section id="examples" className="bg-slate-950 px-5 py-24 text-white lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div className="flex flex-col justify-between gap-6 md:flex-row md:items-end">
            <div>
              <p className="text-sm font-semibold text-blue-400">Before vs after</p>
              <h2 className="mt-4 max-w-2xl text-3xl font-semibold tracking-tight sm:text-5xl">
                The difference is measurable.
              </h2>
            </div>
            <p className="max-w-sm text-sm leading-6 text-slate-400">
              From tangled functions to code your whole team can understand, Code Refine makes progress visible.
            </p>
          </div>

          <div className="mt-14 grid gap-5 lg:grid-cols-2">
            <div>
              <p className="mb-3 text-xs font-medium uppercase tracking-[.18em] text-slate-500">Before Code Refine</p>
              <CodePanel />
              <div className="mt-4 grid grid-cols-4 gap-2 text-center">
                {[['Maintainability', '61'], ['Complexity', '18'], ['Readability', '65'], ['Security', '72']].map(([label, value]) => (
                  <div key={label} className="rounded-lg bg-white/5 p-3">
                    <p className="text-lg font-semibold">{value}</p>
                    <p className="mt-1 text-[10px] text-slate-500">{label}</p>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-3 text-xs font-medium uppercase tracking-[.18em] text-emerald-400">After Code Refine</p>
              <CodePanel after />
              <div className="mt-4 grid grid-cols-4 gap-2 text-center">
                {[['Maintainability', '91'], ['Complexity', '8'], ['Readability', '94'], ['Security', '89']].map(([label, value]) => (
                  <div key={label} className="rounded-lg bg-emerald-400/10 p-3">
                    <p className="text-lg font-semibold text-emerald-300">{value}</p>
                    <p className="mt-1 text-[10px] text-slate-400">{label}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="metrics" className="px-5 py-24 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div className="text-center">
            <p className="text-sm font-semibold text-blue-600">Quality you can see</p>
            <h2 className="mt-4 text-3xl font-semibold tracking-tight sm:text-5xl">Make every improvement count.</h2>
          </div>

          <div className="mt-14 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            {metrics.map(([label, before, after, change]) => (
              <div key={label} className="rounded-2xl border border-slate-200 p-6">
                <p className="text-sm font-medium text-slate-500">{label}</p>
                <div className="mt-6 flex items-baseline gap-2">
                  <span className="text-4xl font-semibold tracking-tight">{before}</span>
                  <ChevronRight className="size-4 text-slate-300" />
                  <span className="text-4xl font-semibold tracking-tight text-blue-600">{after}</span>
                </div>
                <p className="mt-5 flex items-center gap-1 text-sm font-medium text-emerald-600">
                  <TrendingUp className="size-4" /> {change}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section id="features" className="border-y border-slate-100 bg-slate-50/70 px-5 py-24 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div className="max-w-2xl">
            <p className="text-sm font-semibold text-blue-600">Everything your team needs</p>
            <h2 className="mt-4 text-3xl font-semibold tracking-tight sm:text-5xl">
              Quality intelligence that fits your workflow.
            </h2>
          </div>

          <div className="mt-14 grid gap-px overflow-hidden rounded-2xl border border-slate-200 bg-slate-200 sm:grid-cols-2 lg:grid-cols-3">
            {features.map(([title, text, Icon]) => (
              <div key={title} className="bg-white p-7">
                <Icon className="size-5 text-blue-600" />
                <h3 className="mt-8 font-semibold">{title}</h3>
                <p className="mt-3 text-sm leading-6 text-slate-600">{text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="px-5 py-24 lg:px-8">
        <div className="mx-auto max-w-5xl rounded-3xl border border-blue-100 bg-[radial-gradient(circle_at_top_right,rgba(219,234,254,.85),transparent_45%),#f8fbff] px-6 py-12 text-center sm:px-12 sm:py-16">
          <p className="text-sm font-semibold text-blue-600">Built for the new engineering stack</p>
          <div className="mt-10 grid gap-8 text-center sm:grid-cols-4">
            {[
              ['12,480', 'Repositories analyzed'],
              ['84,210', 'Pull requests reviewed'],
              ['6.2M', 'Lines improved'],
              ['420+', 'Teams using Code Refine'],
            ].map(([value, label]) => (
              <div key={label}>
                <p className="text-3xl font-semibold tracking-tight">{value}</p>
                <p className="mt-2 text-xs text-slate-500">{label}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section id="get-started" className="border-t border-slate-100 px-5 py-28 text-center lg:px-8">
        <div className="mx-auto max-w-3xl">
          <h2 className="text-4xl font-semibold tracking-[-.04em] sm:text-6xl">Ship AI-generated code with confidence.</h2>
          <p className="mx-auto mt-6 max-w-xl text-lg leading-8 text-slate-600">
            Automatically measure, compare, and improve code quality before every merge.
          </p>
          <div className="mt-9 flex flex-col justify-center gap-3 sm:flex-row">
            <button className="inline-flex items-center justify-center rounded-full bg-blue-600 px-6 py-3 text-sm font-medium text-white hover:bg-blue-700">
              <GitBranch className="mr-2 size-4" /> Connect GitHub repository
            </button>
            <button className="rounded-full border border-slate-300 px-6 py-3 text-sm font-medium text-slate-700 hover:bg-slate-100">
              Talk to sales <ArrowRight className="ml-1 inline size-4" />
            </button>
          </div>
        </div>
      </section>

      <footer className="border-t border-slate-100 px-5 py-8 lg:px-8">
        <div className="mx-auto flex max-w-7xl flex-col justify-between gap-4 text-sm text-slate-500 sm:flex-row">
          <span className="font-semibold text-slate-900">Code Refine</span>
          <span>© 2026 Code Refine. Quality for the AI era.</span>
          <div className="flex gap-5">
            <a href="#features" className="hover:text-slate-900">Features</a>
            <a href="#pricing" className="hover:text-slate-900">Pricing</a>
            <a href="#get-started" className="hover:text-slate-900">Contact</a>
          </div>
        </div>
      </footer>
    </main>
  );
}
