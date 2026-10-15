let projects = [];

function filterProjects(projectList, technology) {
  if (technology === "all") return projectList;

  return projectList.filter(function(project) {
    return project.technologies.includes(technology);
  });
}

function isWebsiteUrl(value) {
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:";
  } catch {
    return false;
  }
}

function createProjectCard(project) {
  const card = document.createElement("article");
  card.className = "project-box";

  const title = document.createElement("h2");
  title.textContent = project.title;
  card.append(title);

  const technologies = document.createElement("ul");
  technologies.className = "span-box";
  project.technologies.forEach(function(technology) {
    const tag = document.createElement("li");
    tag.textContent = technology;
    technologies.append(tag);
  });
  card.append(technologies);

  const links = document.createElement("div");
  links.className = "project-links";

  if (isWebsiteUrl(project.liveUrl)) {
    const link = document.createElement("a");
    link.className = "project-link";
    link.href = project.liveUrl;
    link.rel = "noopener noreferrer";
    link.target = "_blank";
    link.textContent = "View project ";
    const arrow = document.createElement("span");
    arrow.textContent = "\u2197";
    link.append(arrow);
    links.append(link);
  }
  if (isWebsiteUrl(project.gitHubUrl)) {
    const link = document.createElement("a");
    link.className = "project-link project-link-github";
    link.href = project.gitHubUrl;
    link.rel = "noopener noreferrer";
    link.target = "_blank";
    link.textContent = "View GitHub ";
    const arrow = document.createElement("span");
    arrow.textContent = "\u2197";
    link.append(arrow);
    links.append(link);
  }
  if (links.childElementCount > 0) {
    card.append(links);
  }

  return card;
}

function renderProjects(projectList) {
  const container = document.getElementById("projects-list");
  container.replaceChildren();
  projectList.forEach(function(project) {
    container.append(createProjectCard(project));
  });

  document.getElementById("project-count").textContent = projectList.length === 0
    ? "No projects found for this technology."
    : projectList.length + (projectList.length === 1 ? " project" : " projects");
}

function populateTechnologyFilter(projectList) {
  const select = document.getElementById("technology-filter");
  const technologies = new Set();
  projectList.forEach(function(project) {
    project.technologies.forEach(function(technology) {
      technologies.add(technology);
    });
  });

  select.replaceChildren(new Option("All technologies", "all"));
  Array.from(technologies).sort().forEach(function(technology) {
    select.add(new Option(technology, technology));
  });
}

function handleProjectFilter(event) {
  const filteredProjects = filterProjects(projects, event.target.value);
  renderProjects(filteredProjects);
}

async function initializeProjects() {
  const select = document.getElementById("technology-filter");
  if (!select) return;

  const status = document.getElementById("project-count");
  status.textContent = "Loading projects…";
  try {
    projects = await fetchCollection("projects");
    populateTechnologyFilter(projects);
    renderProjects(projects);
    if (projects.length === 0) {
      status.textContent = "No projects have been published yet.";
    }
    select.addEventListener("change", handleProjectFilter);
    select.disabled = projects.length === 0;
  } catch {
    document.getElementById("projects-list").replaceChildren();
    status.textContent = "Projects could not be loaded. Please try again later.";
    select.disabled = true;
  }
}

initializeProjects();
