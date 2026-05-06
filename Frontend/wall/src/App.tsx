import { BrowserRouter, Route, Routes } from 'react-router-dom'
import PostUploadPage from './PostUploadPage'
import WallScreen from './WallScreen'

function Home() {
  return (
    <div style={{ padding: '2rem', fontFamily: 'system-ui', color: '#e5e7eb', background: '#070712', minHeight: '100vh' }}>
      <h1 style={{ marginTop: 0 }}>Hashtag Wall</h1>
      <p>
        Abra o mural em{' '}
        <code>/wall/&lt;sua-hashtag&gt;</code> — exemplo: <code>/wall/meuevento</code>.
      </p>
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/wall/:hashtag" element={<WallScreen />} />
        <Route path="/wall/:hashtag/post" element={<PostUploadPage />} />
        <Route path="/" element={<Home />} />
      </Routes>
    </BrowserRouter>
  )
}
