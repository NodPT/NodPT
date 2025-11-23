<template>
  <div class="landing-page" :data-theme="isDarkTheme ? 'dark' : 'light'">
    <header :class="['landing-header sticky-top py-2', { scrolled: isScrolled }]">
      <div class="container">
        <div class="d-flex flex-column flex-lg-row align-items-center justify-content-between gap-3">
          <div class="d-flex align-items-center gap-2">
            <h1 class="h3 fw-bold mb-0">
              <span class="gradient-text"><img class="rounded-3" src="/images/logo-small.png" alt="NodPT" /></span>
            </h1>
            <span class="text-white-50 small">Intelligence, Multiplied.</span>
          </div>
          <nav class="d-flex flex-wrap align-items-center gap-3">
            <a href="https://github.com/NodPT" target="_blank" rel="noopener noreferrer"
              class="btn btn-info btn-sm px-3">
              <i class="bi bi-github"></i>
            </a>
            <a href="#support" class="btn btn-warning btn-sm px-3">
              <i class="bi bi-heart-fill me-2"></i>Support
            </a>
            <router-link to="/login" class="btn btn-success btn-sm px-3">Login</router-link>
            <button @click="toggleTheme" class="btn btn-sm text-white"
              :title="isDarkTheme ? 'Switch to Light Theme' : 'Switch to Dark Theme'">
              <i :class="isDarkTheme ? 'bi bi-sun-fill' : 'bi bi-moon-fill'"></i>
            </button>
          </nav>
        </div>
      </div>
    </header>

    <section class="landing-hero position-relative d-flex align-items-center text-center text-lg-start py-5">
      <div class="hero-background">
        <div class="ai-particles" ref="particlesContainer"></div>
      </div>
      <div class="container position-relative">
        <div class="row align-items-center gy-5">
          <div class="col-lg-7">
            <h1 class="display-4 fw-bold mb-4">
              You don't work with one AI &mdash;<br>
              <span class="gradient-text">you lead an entire AI team.</span>
            </h1>
            <p class="lead text-white-50 mb-4">
              A limitless network of AI minds that think, talk, and collaborate &mdash; turning your ideas into real
              results across every domain: music, research, writing, design, coding, and more.
            </p>
            <div class="d-flex flex-column flex-sm-row gap-3">
              <button class="btn btn-primary btn-sm px-4" @click="navigateToProject">
                Start Your AI Team
              </button>
              <button class="btn btn-sm px-4 btn-info" @click="scrollToUseCases">
                Explore Use Cases
              </button>
              <button class="btn text-white btn-sm px-4 py-3 position-relative overflow-hidden" @click="scrollToVideo">
                <span class="position-relative z-1">
                  <i class="bi bi-play-circle-fill me-2"></i>
                  See how it works
                </span>
                <span class="btn-pulse"></span>
              </button>
            </div>

          </div>
          <div class="col-lg-5">
            <div class="card bg-transparent border border-light border -opacity-25 shadow-sm">
              <div class="card-body d-flex flex-column gap-3 text-start">
                <p class="text-uppercase text-white-50 small mb-0">Why teams choose NodPT</p>
                <div class="d-flex align-items-start gap-3">
                  <i class="bi bi-lightning-charge-fill text-warning fs-2"></i>
                  <div>
                    <h5 class="fw-semibold mb-1 text-white">Launch multi-agent flows fast</h5>
                    <p class="mb-0 text-white-50">Spin up orchestrated AI teams in minutes with reusable templates.</p>
                  </div>
                </div>
                <div class="d-flex align-items-start gap-3">
                  <i class="bi bi-people-fill text-info fs-2"></i>
                  <div>
                    <h5 class="fw-semibold mb-1 text-white">Collaborate in real time</h5>
                    <p class="mb-0 text-white-50">Track decisions, share context, and keep every agent in sync.</p>
                  </div>
                </div>
                <div class="d-flex align-items-start gap-3">
                  <i class="bi bi-bar-chart-fill text-success fs-2"></i>
                  <div>
                    <h5 class="fw-semibold mb-1 text-white">Measure every iteration</h5>
                    <p class="mb-0 text-white-50">Built-in insight panels highlight quality, speed, and outcomes.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="row mt-5">
          <div class="col-12">
            <div id="video-demo" class="mx-auto w-100 pt-3" ref="videoContainer">
              <div class="ratio ratio-16x9">
                <video autoplay muted loop>
                  <source src="/videos/coming_soon.mp4" type="video/mp4" />
                  Your browser does not support the video tag.
                </video>
                <!-- <iframe 
                  ref="videoIframe"
                  src="https://www.youtube.com/embed/MzEFeIRJ0eQ?si=BZI9qKboNqZM4d9E&controls=0&modestbranding=1&rel=0&showinfo=0&enablejsapi=1" 
                  title="NodPT Demo Video" 
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" 
                  allowfullscreen
                  class="rounded-4"
                  style="border: 0; width: 100%;"
                ></iframe> -->
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section id="about" class="py-1">
      <div class="container">
        <div class="row align-items-center gy-5">
          <div class="col-lg-6">
            <div class="card bg-transparent border-0">
              <div class="card-body p-4">
                <h2 class="display-6 fw-bold mb-4">Not one AI, but a <span class="gradient-text">multitasking
                    intelligence</span></h2>
                <p class="text-white-50 mb-4">
                  Each node understands your goals and every other node in the workflow. They collaborate instantly,
                  share progress, and adapt as you refine instructions.
                </p>
                <ul class="list-unstyled text-white-50 mb-0">
                  <li class="d-flex align-items-center gap-3 mb-3">
                    <i class="bi bi-cpu-fill fs-4 text-primary"></i>
                    <span>Specialized agents that speak the same language</span>
                  </li>
                  <li class="d-flex align-items-center gap-3 mb-3">
                    <i class="bi bi-share-fill fs-4 text-info"></i>
                    <span>Shared memory keeps results and context in sync</span>
                  </li>
                  <li class="d-flex align-items-center gap-3">
                    <i class="bi bi-clock-history fs-4 text-warning"></i>
                    <span>No waiting &mdash; work streams run side by side</span>
                  </li>
                </ul>
              </div>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="card bg-transparent border-0">
              <div class="card-body">
                <h5 class="text-uppercase text-white-50 fw-semibold mb-3">Popular combinations</h5>
                <div class="row g-3">
                  <div class="col-sm-6">
                    <div class="border border-light border-opacity-25 rounded-4 p-3 h-100">
                      <p class="mb-1 text-white fw-semibold">Product Launch</p>
                      <p class="small mb-0 text-white-50">Research, writing, and design agents power go-to-market plans.
                      </p>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="border border-light border-opacity-25 rounded-4 p-3 h-100">
                      <p class="mb-1 text-white fw-semibold">Code &amp; QA Loop</p>
                      <p class="small mb-0 text-white-50">Generation, static analysis, and test agents coordinate
                        delivery.</p>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="border border-light border-opacity-25 rounded-4 p-3 h-100">
                      <p class="mb-1 text-white fw-semibold">Studio Production</p>
                      <p class="small mb-0 text-white-50">Music, script, and video agents keep creative workflows
                        moving.</p>
                    </div>
                  </div>
                  <div class="col-sm-6">
                    <div class="border border-light border-opacity-25 rounded-4 p-3 h-100">
                      <p class="mb-1 text-white fw-semibold">Executive Briefings</p>
                      <p class="small mb-0 text-white-50">Research, insight, and writing agents prepare
                        stakeholder-ready decks.</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section id="vision" class="py-5">
      <div class="container">
        <div class="row align-items-center gy-4">
          <div class="col-lg-10 mx-auto">
            <div class="text-center mb-5">
              <h2 class="display-6 fw-bold mb-4">Our <span class="gradient-text">Vision</span></h2>
              <p class="lead text-white-50">
                Why NodPT exists and how you can be part of it
              </p>
            </div>

            <div class="card bg-transparent border-light border-opacity-25 shadow-sm mb-4">
              <div class="card-body p-4">
                <h5 class="text-white fw-semibold mb-3">
                  <i class="bi bi-lightbulb-fill text-warning me-2"></i>
                  The Problem We're Solving
                </h5>
                <p class="text-white-50 mb-0">
                  AI tools today are scattered, siloed, and limited. You're forced to jump between different platforms,
                  copy-paste results, and manually coordinate tasks that should flow naturally together. We believe AI
                  should work the way humans do—as a collaborative team where each member brings unique expertise and
                  they all communicate seamlessly.
                </p>
              </div>
            </div>

            <div class="card bg-transparent border-light border-opacity-25 shadow-sm mb-4">
              <div class="card-body p-4">
                <h5 class="text-white fw-semibold mb-3">
                  <i class="bi bi-stars text-info me-2"></i>
                  Our Mission
                </h5>
                <p class="text-white-50 mb-0">
                  NodPT is building the future of AI collaboration—a visual, node-based platform where AI agents work
                  together as an intelligent team. We're creating an open-source ecosystem that democratizes access to
                  multi-agent AI workflows, making it easy for anyone to orchestrate complex tasks through simple
                  visual connections.
                </p>
              </div>
            </div>

            <div class="card bg-transparent border-light border-opacity-25 shadow-sm mb-4">
              <div class="card-body p-4">
                <h5 class="text-white fw-semibold mb-3">
                  <i class="bi bi-heart-fill text-danger me-2"></i>
                  Join the Movement
                </h5>
                <p class="text-white-50 mb-3">
                  We're an open-source project driven by the belief that powerful AI tools should be accessible to
                  everyone. Whether you're a developer, designer, writer, or domain expert—your contribution matters.
                </p>
                <div class="row g-3">
                  <div class="col-md-6">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-code-slash text-primary fs-4"></i>
                      <div>
                        <p class="text-white fw-semibold mb-1">Contribute Code</p>
                        <p class="text-white-50 small mb-0">Help build features, fix bugs, or improve performance</p>
                      </div>
                    </div>
                  </div>
                  <div class="col-md-6">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-brush text-success fs-4"></i>
                      <div>
                        <p class="text-white fw-semibold mb-1">Design & UX</p>
                        <p class="text-white-50 small mb-0">Enhance the user experience and visual design</p>
                      </div>
                    </div>
                  </div>
                  <div class="col-md-6">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-book text-warning fs-4"></i>
                      <div>
                        <p class="text-white fw-semibold mb-1">Documentation</p>
                        <p class="text-white-50 small mb-0">Write guides, tutorials, and API documentation</p>
                      </div>
                    </div>
                  </div>
                  <div class="col-md-6">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-chat-dots text-info fs-4"></i>
                      <div>
                        <p class="text-white fw-semibold mb-1">Community</p>
                        <p class="text-white-50 small mb-0">Share ideas, help others, and spread the word</p>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="text-center mt-4">
                  <a href="https://github.com/NodPT" target="_blank" rel="noopener noreferrer"
                    class="btn btn-primary btn-lg px-5">
                    <i class="bi bi-github me-2"></i>Start Contributing on GitHub
                  </a>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section id="support" class="py-5">
      <div class="container">
        <div class="row justify-content-center">
          <div class="col-lg-10">
            <div class="text-center mb-5">
              <h2 class="display-6 fw-bold mb-4">Why We Need <span class="gradient-text">Your Support</span></h2>
              <p class="lead text-white-50">
                Help us build the infrastructure to power the future of AI collaboration
              </p>
            </div>

            <div class="card bg-transparent border-light border-opacity-25 shadow-sm mb-4">
              <div class="card-body p-4">
                <h5 class="text-white fw-semibold mb-3">
                  <i class="bi bi-server text-primary me-2"></i>
                  Building Our AI Infrastructure
                </h5>
                <p class="text-white-50 mb-3">
                  To deliver the best multi-agent AI experience, we need substantial computing power. Training and
                  running massive Large Language Models (LLMs) requires dedicated server infrastructure with powerful
                  GPUs and high-bandwidth networking capabilities.
                </p>
                <p class="text-white-50 mb-0">
                  Your financial support directly enables us to:
                </p>
              </div>
            </div>

            <div class="row g-4 mb-4">
              <div class="col-md-6">
                <div class="card bg-transparent border-light border-opacity-25 h-100">
                  <div class="card-body p-4">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-hdd-rack-fill text-info fs-3"></i>
                      <div>
                        <h6 class="text-white fw-semibold mb-2">Scale Our Server Capacity</h6>
                        <p class="text-white-50 small mb-0">
                          Acquire and maintain high-performance GPU servers capable of training and serving
                          state-of-the-art LLM models for the community.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div class="col-md-6">
                <div class="card bg-transparent border-light border-opacity-25 h-100">
                  <div class="card-body p-4">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-speedometer2 text-success fs-3"></i>
                      <div>
                        <h6 class="text-white fw-semibold mb-2">Improve Performance</h6>
                        <p class="text-white-50 small mb-0">
                          Optimize response times and ensure smooth, real-time collaboration between AI agents
                          even during peak usage.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div class="col-md-6">
                <div class="card bg-transparent border-light border-opacity-25 h-100">
                  <div class="card-body p-4">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-shield-check text-warning fs-3"></i>
                      <div>
                        <h6 class="text-white fw-semibold mb-2">Keep It Free & Open</h6>
                        <p class="text-white-50 small mb-0">
                          Maintain free access to core features while keeping the project open-source and
                          community-driven for everyone.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div class="col-md-6">
                <div class="card bg-transparent border-light border-opacity-25 h-100">
                  <div class="card-body p-4">
                    <div class="d-flex align-items-start gap-3">
                      <i class="bi bi-rocket-takeoff-fill text-danger fs-3"></i>
                      <div>
                        <h6 class="text-white fw-semibold mb-2">Accelerate Development</h6>
                        <p class="text-white-50 small mb-0">
                          Fund ongoing research, development, and innovation to bring new AI capabilities
                          and features to the platform faster.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="text-center">
              <a href="#support" class="btn btn-warning btn-lg px-5 fw-semibold">
                <i class="bi bi-heart-fill me-2"></i>Support Our Mission
              </a>
              <p class="text-white-50 small mt-3 mb-0">
                Every contribution, big or small, helps us build a better AI future for everyone.
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section id="use-cases" class="py-5">
      <div class="container">
        <h2 class="display-6 fw-bold text-center mb-5">Unlimited ideas, <span class="gradient-text">one platform</span>
        </h2>
        <div class="row g-4 use-case-grid">
          <div v-for="useCase in useCases" :key="useCase.id" class="col-md-6 col-lg-4">
            <div class="card h-100 text-center shadow-sm">
              <div class="card-body d-flex flex-column gap-3">
                <div class="display-5">
                  <i :class="useCase.icon"></i>
                </div>
                <h5 class="fw-semibold text-white mb-1">{{ useCase.title }}</h5>
                <p class="text-white-50 mb-0">{{ useCase.description }}</p>
                <div class="mt-auto">
                  <button class="btn btn-light btn-sm px-3" @click="tryScenario(useCase)">
                    Try Scenario
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section id="contact" class="trust-section py-5">
      <div class="container">
        <div class="text-center">
          <h2 class="display-6 fw-bold mb-4">From A to Z. <span class="gradient-text">No chaos.</span></h2>
          <p class="text-white-50 lead mb-4">
            Plan, create, test, and refine without losing control. Your AI team monitors itself, ensuring progress from
            start to finish.
          </p>
          <p class="text-white mb-0 fw-semibold">Need to talk to the team?</p>
          <p class="text-white-50 mb-4">Drop us a note and we will assemble the right agents for you.</p>
          <button class="btn btn-light px-4" @click="navigateToProject">
            Talk to Solutions
          </button>
        </div>
      </div>
    </section>

    <section class="final-cta-section py-5">
      <div class="container">
        <div class="text-center">
          <h2 class="display-5 fw-bold mb-4">One click. Infinite minds. <span class="gradient-text">Your AI team
              awaits.</span></h2>
          <button class="btn btn-primary btn-lg px-5" @click="navigateToProject">
            Get Started for Free
          </button>
        </div>
      </div>
    </section>

    <footer class="landing-footer py-4">
      <div class="container">
        <div class="row gy-3 align-items-center">
          <div class="col-md-4 text-center text-md-start">
            <h3 class="h4 fw-bold mb-1"><span class="gradient-text">NodPT</span></h3>
            <p class="text-white-50 mb-0">Intelligence, Multiplied.</p>
          </div>
          <div class="col-md-4 d-flex flex-column flex-md-row justify-content-center gap-3">
            <router-link to="/terms" class="landing-nav-link">Terms</router-link>
            <a href="#privacy" class="landing-nav-link">Privacy</a>
            <a href="#support" class="landing-nav-link">Support</a>
          </div>
          <div class="col-md-4 text-center text-md-end text-white-50">
            &copy; 2025 NodPT. All rights reserved.
          </div>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useTheme } from '../composables/useTheme';

const router = useRouter();
const isScrolled = ref(false);
const particlesContainer = ref(null);
const videoContainer = ref(null);
const videoIframe = ref(null);

// Theme management
const { isDarkTheme, toggleTheme, loadTheme } = useTheme();

const useCases = ref([
  {
    id: 1,
    title: 'Coding',
    description: 'Build, test, and deploy software with AI-powered development.',
    icon: 'fas fa-code'
  },
  {
    id: 2,
    title: 'Music & Sound Design',
    description: 'Compose, mix, and master tracks collaboratively.',
    icon: 'fas fa-music'
  },
  {
    id: 3,
    title: 'Book Writing & Publishing',
    description: 'From concept to editing to illustration.',
    icon: 'fas fa-book'
  },
  {
    id: 4,
    title: 'Research & Analysis',
    description: 'Summarize studies, test hypotheses, and visualize data.',
    icon: 'fas fa-microscope'
  },
  {
    id: 5,
    title: 'Creative Design',
    description: 'Generate visuals, branding, or 3D concepts.',
    icon: 'fas fa-palette'
  },
  {
    id: 6,
    title: 'Custom Logic Workflows',
    description: 'Connect your own AI nodes and automate anything.',
    icon: 'fas fa-project-diagram'
  }
]);

// Scroll handler
const handleScroll = () => {
  isScrolled.value = window.scrollY > 50;
};

// Navigation functions
const navigateToProject = () => {
  router.push('/project');
};

const scrollToUseCases = () => {
  const element = document.getElementById('use-cases');
  if (element) {
    element.scrollIntoView({ behavior: 'smooth' });
  }
};

const scrollToVideo = () => {
  const element = document.getElementById('video-demo');
  if (element) {
    const elementPosition = element.getBoundingClientRect().top + window.scrollY;
    const offsetPosition = elementPosition - 100;

    window.scrollTo({
      top: offsetPosition,
      behavior: 'smooth'
    });
  }
};

const tryScenario = (useCase) => {
  console.log('Trying scenario:', useCase.title);
  router.push('/project');
};

// Particle animation
const createParticles = () => {
  if (!particlesContainer.value) return;

  const particleCount = 50;
  for (let i = 0; i < particleCount; i++) {
    const particle = document.createElement('div');
    particle.className = 'particle';
    particle.style.left = Math.random() * 100 + '%';
    particle.style.top = Math.random() * 100 + '%';
    particle.style.animationDelay = Math.random() * 5 + 's';
    particle.style.animationDuration = (Math.random() * 10 + 10) + 's';
    particlesContainer.value.appendChild(particle);
  }
};

onMounted(() => {
  // Load saved theme preference or default to dark
  loadTheme();

  window.addEventListener('scroll', handleScroll);
  createParticles();

  // Setup Intersection Observer for video autoplay
  if (videoContainer.value && videoIframe.value) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            // Auto-play the video when it comes into view
            const iframe = videoIframe.value;
            const src = iframe.src;
            if (!src.includes('autoplay=1')) {
              iframe.src = src + (src.includes('?') ? '&' : '?') + 'autoplay=1&mute=1';
            }
          }
        });
      },
      { threshold: 0.5 } // Trigger when 50% of the video is visible
    );
    observer.observe(videoContainer.value);
  }
});

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll);
});
</script>

<style>
/* Import dark theme by default */
@import '../assets/styles/landing-page-dark.css';
</style>

<style>
/* Import light theme overrides */
@import '../assets/styles/landing-page-light-scoped.css';
</style>
