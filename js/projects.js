const projects = [
  {
    name: "Yona Group",
    technologies: ["JavaScript", "HTML", "CSS"],
    url: "https://www.yonagroup.nl/"
  },
  {
    name: "Scutis",
    technologies: ["TypeScript", "Next.js", "Tailwind CSS"],
    url: "https://scutis.nl/"
  },
  {
    name: "Dialoog Delft",
    technologies: ["JavaScript", "HTML", "CSS"],
    url: "https://dialoogdelft.netlify.app/"
  },
  {
    name: "SOC Management System",
    technologies: ["Next.js", "C#", "TypeScript"],
    url: null
  }
];

function filterProjects(projectList, technology) {
  if (technology === "all") return projectList;

  return projectList.filter(function(project) {
    return project.technologies.includes(technology);
  });
}

function createProjectCard(project) {
  const card = document.createElement("article");
  card.className = "project-box";

  const title = document.createElement("h2");
  title.textContent = project.name;
  card.append(title);

  const technologies = document.createElement("div");
  technologies.className = "span-box";
  project.technologies.forEach(function(technology) {
    const tag = document.createElement("span");
    tag.textContent = technology;
    technologies.append(tag);
  });
  card.append(technologies);

  if (project.url) {
    const link = document.createElement("a");
    link.className = "project-link";
    link.href = project.url;
    link.target = "_blank";
    link.textContent = "View project ";
    const arrow = document.createElement("span");
    arrow.textContent = "\u2197";
    link.append(arrow);
    card.append(link);
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

function initializeProjects() {
  const select = document.getElementById("technology-filter");
  if (!select) return;

  populateTechnologyFilter(projects);
  renderProjects(projects);
  select.addEventListener("change", handleProjectFilter);
  select.disabled = false;
}

initializeProjects();
