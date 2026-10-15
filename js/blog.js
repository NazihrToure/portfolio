function createBlogpostCard(blogpost, index) {
  const card = document.createElement("article");
  card.className = "post-card";
  card.id = `blogpost-${blogpost.id}`;

  const meta = document.createElement("div");
  meta.className = "post-meta";
  const number = document.createElement("span");
  number.textContent = String(index + 1).padStart(2, "0");
  const category = document.createElement("span");
  category.textContent = blogpost.category;
  meta.append(number, category);

  const body = document.createElement("div");
  body.className = "post-body";
  const title = document.createElement("h3");
  title.textContent = blogpost.title;
  const introduction = document.createElement("p");
  introduction.textContent = blogpost.summary;

  const details = document.createElement("details");
  const summary = document.createElement("summary");
  summary.textContent = "Read note ";
  const arrow = document.createElement("span");
  arrow.textContent = "↗";
  summary.append(arrow);

  const content = document.createElement("div");
  content.className = "post-expanded";
  blogpost.content.split(/\r?\n\s*\r?\n/).filter(text => text.trim()).forEach(text => {
    const paragraph = document.createElement("p");
    paragraph.textContent = text;
    content.append(paragraph);
  });

  details.append(summary, content);
  body.append(title, introduction, details);
  card.append(meta, body);
  return card;
}

async function initializeBlogposts() {
  const container = document.getElementById("blogposts-list");
  if (!container) return;

  const status = document.getElementById("blog-status");
  status.textContent = "Loading posts…";
  try {
    const blogposts = await fetchCollection("blogposts");
    container.replaceChildren(...blogposts.map(createBlogpostCard));
    status.textContent = blogposts.length === 0
      ? "No posts have been published yet."
      : `${blogposts.length} ${blogposts.length === 1 ? "post" : "posts"}`;
  } catch {
    container.replaceChildren();
    status.textContent = "Posts could not be loaded. Please try again later.";
  }
}

initializeBlogposts();
