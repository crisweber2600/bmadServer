import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import { ChatDemo } from './ChatDemo.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ChatDemo />
  </StrictMode>,
)
