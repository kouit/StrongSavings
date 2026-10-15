
import { useState } from 'react'
import './App.css'

const navigation = [
  { name: 'Dashboard', icon: '◫' },
  { name: 'Groups', icon: '◉' },
  { name: 'Members', icon: '♙' },
  { name: 'Accounts', icon: '▤' },
  { name: 'Transactions', icon: '⇄' },
  { name: 'Meetings', icon: '▦' },
]

const transactions = [
  { name: 'Weekly club contribution', person: 'Sample member A', type: 'Deposit', date: '11 Oct 2026', amount: 100 },
  { name: 'Weekly club contribution', person: 'Sample member B', type: 'Deposit', date: '11 Oct 2026', amount: 50 },
  { name: 'Club expenses', person: 'Sample administrator', type: 'Withdrawal', date: '10 Oct 2026', amount: -25 },
  { name: 'Weekly club contribution', person: 'Sample member C', type: 'Deposit', date: '10 Oct 2026', amount: 100 },
]

const money = (amount: number) =>
  new Intl.NumberFormat('en-NZ', {
    style: 'currency',
    currency: 'NZD',
    maximumFractionDigits: 2,
  }).format(amount)

function App() {
  const [activePage, setActivePage] = useState('Dashboard')
  const [search, setSearch] = useState('')

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">S</div>
          <div>
            <strong>StrongSavings</strong>
            <span>Club management</span>
          </div>
        </div>

        <div className="nav-label">WORKSPACE</div>
        <nav className="navigation">
          {navigation.map((item) => (
            <button
              key={item.name}
              className={`nav-item ${activePage === item.name ? 'active' : ''}`}
              onClick={() => setActivePage(item.name)}
            >
              <span className="nav-icon">{item.icon}</span>
              <span>{item.name}</span>
              {item.name === 'Transactions' && <span className="nav-arrow">›</span>}
            </button>
          ))}
        </nav>

        <div className="sidebar-bottom">
          <div className="help-card">
            <div className="help-icon">?</div>
            <strong>Need a hand?</strong>
            <p>Your club savings, organised in one place.</p>
          </div>
          <div className="profile">
            <div className="avatar">TM</div>
            <div className="profile-info">
              <strong>Club administrator</strong>
              <span>Administrator</span>
            </div>
            <span className="profile-menu">•••</span>
          </div>
        </div>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div className="breadcrumbs">
            <span>Workspace</span><span>/</span><strong>{activePage}</strong>
          </div>
          <div className="topbar-right">
            <span className="environment-badge">
              <span className="status-dot" /> Preview
            </span>
            <div className="topbar-avatar">TM</div>
          </div>
        </header>

        <div className="page-content">
          <section className="welcome-row">
            <div>
              <div className="eyebrow">YOUR CLUB AT A GLANCE</div>
              <h1>{activePage === 'Dashboard' ? 'Good to see you' : activePage}</h1>
              <p className="subtitle">
                {activePage === 'Dashboard'
                  ? 'Here’s an overview of your savings club.'
                  : `Manage your club’s ${activePage.toLowerCase()} in one place.`}
              </p>
            </div>
            <div className="date-chip">◷ &nbsp; October 2026</div>
          </section>

          {activePage === 'Dashboard' ? (
            <>
              <section className="hero-card">
                <div className="hero-copy">
                  <div className="hero-label"><span /> CLUB SAVINGS OVERVIEW</div>
                  <h2>Building stronger<br />savings together.</h2>
                  <p>Keep track of every contribution and every step towards your goals.</p>
                  <button className="hero-button" onClick={() => setActivePage('Transactions')}>
                    View transactions <span>→</span>
                  </button>
                </div>
                <div className="hero-art" aria-hidden="true">
                  <div className="art-circle circle-one" />
                  <div className="art-circle circle-two" />
                  <div className="coin coin-one">$</div>
                  <div className="coin coin-two">$</div>
                  <div className="art-wallet">
                    <div className="wallet-line" />
                    <div className="wallet-pocket"><span /></div>
                  </div>
                  <div className="art-leaf leaf-one">⌁</div>
                  <div className="art-leaf leaf-two">⌁</div>
                </div>
                <div className="hero-decoration">S</div>
              </section>

              <section className="stats-grid">
                <article className="stat-card">
                  <div className="stat-top">
                    <span>Total club savings</span>
                    <span className="stat-icon green">▤</span>
                  </div>
                  <div className="stat-value">{money(0)}</div>
                  <div className="stat-foot">Connect API to load real balance</div>
                </article>
                <article className="stat-card">
                  <div className="stat-top">
                    <span>Club members</span>
                    <span className="stat-icon purple">♙</span>
                  </div>
                  <div className="stat-value">—</div>
                  <div className="stat-foot">Member count not loaded yet</div>
                </article>
                <article className="stat-card">
                  <div className="stat-top">
                    <span>Lalo Apele</span>
                    <span className="stat-icon gold">◉</span>
                  </div>
                  <div className="stat-value">{money(0)}</div>
                  <div className="stat-foot">General account · Awaiting API</div>
                </article>
                <article className="stat-card">
                  <div className="stat-top">
                    <span>Recent activity</span>
                    <span className="stat-icon blue">⇄</span>
                  </div>
                  <div className="stat-value">—</div>
                  <div className="stat-foot">Transaction count not loaded yet</div>
                </article>
              </section>

              <section className="content-grid">
                <article className="panel transactions-panel">
                  <div className="panel-heading">
                    <div>
                      <h3>Recent transactions</h3>
                      <p>A preview of the transaction table layout</p>
                    </div>
                    <button className="text-button" onClick={() => setActivePage('Transactions')}>
                      View all <span>→</span>
                    </button>
                  </div>

                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr>
                          <th>TRANSACTION</th>
                          <th>DATE</th>
                          <th>TYPE</th>
                          <th className="amount-column">AMOUNT</th>
                        </tr>
                      </thead>
                      <tbody>
                        {transactions.map((transaction, index) => (
                          <tr key={`${transaction.person}-${index}`}>
                            <td>
                              <div className="transaction-name">
                                <span className={`transaction-symbol ${transaction.amount < 0 ? 'withdrawal' : ''}`}>
                                  {transaction.amount < 0 ? '↗' : '↙'}
                                </span>
                                <div>
                                  <strong>{transaction.name}</strong>
                                  <span>{transaction.person}</span>
                                </div>
                              </div>
                            </td>
                            <td className="date-cell">{transaction.date}</td>
                            <td>
                              <span className={`type-pill ${transaction.amount < 0 ? 'type-out' : 'type-in'}`}>
                                {transaction.type}
                              </span>
                            </td>
                            <td className={`amount-column amount ${transaction.amount < 0 ? 'negative' : ''}`}>
                              {transaction.amount < 0 ? '−' : '+'}{money(Math.abs(transaction.amount))}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <div className="demo-note">
                    Preview only — these transactions are examples, not your club's records.
                  </div>
                </article>

                <article className="panel quick-panel">
                  <div className="panel-heading">
                    <div>
                      <h3>Quick actions</h3>
                      <p>Navigate to a club task</p>
                    </div>
                  </div>
                  <button className="quick-action" onClick={() => setActivePage('Members')}>
                    <span className="quick-icon member-icon">♙</span>
                    <span><strong>Manage members</strong><small>Browse club members</small></span>
                    <span className="quick-arrow">→</span>
                  </button>
                  <button className="quick-action" onClick={() => setActivePage('Groups')}>
                    <span className="quick-icon group-icon">◉</span>
                    <span><strong>View groups</strong><small>Explore your club groups</small></span>
                    <span className="quick-arrow">→</span>
                  </button>
                  <button className="quick-action" onClick={() => setActivePage('Accounts')}>
                    <span className="quick-icon account-icon">▤</span>
                    <span><strong>View accounts</strong><small>Personal and general accounts</small></span>
                    <span className="quick-arrow">→</span>
                  </button>
                  <button className="quick-action" onClick={() => setActivePage('Meetings')}>
                    <span className="quick-icon meeting-icon">▦</span>
                    <span><strong>Club meetings</strong><small>Review meeting records</small></span>
                    <span className="quick-arrow">→</span>
                  </button>
                </article>
              </section>
            </>
          ) : (
            <section className="panel placeholder-panel">
              <div className="placeholder-icon">✦</div>
              <h2>{activePage}</h2>
              <p>This screen is ready for the next stage of development.</p>
              <p className="placeholder-help">
                Next, we’ll connect this page to your StrongSavings API so it can display and manage real club data.
              </p>
              {activePage === 'Members' && (
                <input
                  className="search-input"
                  placeholder="Search members (preview)"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                />
              )}
              <button className="primary-button" onClick={() => setActivePage('Dashboard')}>
                ← Back to dashboard
              </button>
            </section>
          )}

          <footer className="footer">
            <span>StrongSavings <span className="footer-dot">•</span> Club management</span>
            <span>Built for your savings journey</span>
          </footer>
        </div>
      </main>
    </div>
  )
}

export default App
