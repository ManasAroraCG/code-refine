export const formatDate = (date) =>
  new Date(date).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });

export const truncate = (text, length = 100) =>
  text && text.length > length ? `${text.slice(0, length)}...` : text;
