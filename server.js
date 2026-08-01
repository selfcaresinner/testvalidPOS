const http = require('http');

const server = http.createServer((req, res) => {
  res.statusCode = 200;
  res.setHeader('Content-Type', 'text/plain');
  res.end('The backend is deployed to Railway and the frontend is a WPF application.\n');
});

const port = process.env.PORT || 3000;
server.listen(port, () => {
  console.log(`Dummy server running at port ${port}`);
});
