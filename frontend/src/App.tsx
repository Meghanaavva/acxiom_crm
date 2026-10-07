import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import Login from './pages/auth/Login';
import Register from './pages/auth/Register';
import ChangePassword from './pages/auth/ChangePassword';
import Dashboard from './pages/dashboard/Dashboard';
import CustomerList from './pages/customers/CustomerList';
import CustomerForm from './pages/customers/CustomerForm';
import CustomerDetails from './pages/customers/CustomerDetails';
import LeadList from './pages/leads/LeadList';
import LeadForm from './pages/leads/LeadForm';
import LeadDetails from './pages/leads/LeadDetails';
import OpportunityList from './pages/opportunities/OpportunityList';
import OpportunityForm from './pages/opportunities/OpportunityForm';
import OpportunityDetails from './pages/opportunities/OpportunityDetails';
import PipelineBoard from './pages/opportunities/PipelineBoard';
import FollowUpList from './pages/followups/FollowUpList';
import FollowUpForm from './pages/followups/FollowUpForm';
import ActivityList from './pages/activities/ActivityList';
import ActivityForm from './pages/activities/ActivityForm';
import UserList from './pages/users/UserList';
import UserForm from './pages/users/UserForm';
import RolesPage from './pages/users/RolesPage';
import AuditLogList from './pages/audit/AuditLogList';
import Reports from './pages/reports/Reports';
import NotFound from './pages/NotFound';
import SearchResults from './pages/SearchResults';
import Forbidden from './pages/Forbidden';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />

      {/* Any logged in user */}
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/change-password" element={<ChangePassword />} />

          <Route path="/customers" element={<CustomerList />} />
          <Route path="/customers/new" element={<CustomerForm />} />
          <Route path="/customers/:id" element={<CustomerDetails />} />
          <Route path="/customers/:id/edit" element={<CustomerForm />} />

          <Route path="/leads" element={<LeadList />} />
          <Route path="/leads/new" element={<LeadForm />} />
          <Route path="/leads/:id" element={<LeadDetails />} />
          <Route path="/leads/:id/edit" element={<LeadForm />} />

          <Route path="/opportunities" element={<OpportunityList />} />
          <Route path="/opportunities/new" element={<OpportunityForm />} />
          <Route path="/opportunities/:id" element={<OpportunityDetails />} />
          <Route path="/opportunities/:id/edit" element={<OpportunityForm />} />
          <Route path="/pipeline" element={<PipelineBoard />} />

          <Route path="/followups" element={<FollowUpList />} />
          <Route path="/followups/new" element={<FollowUpForm />} />
          <Route path="/followups/:id/edit" element={<FollowUpForm />} />

          <Route path="/activities" element={<ActivityList />} />
          <Route path="/activities/new" element={<ActivityForm />} />
          <Route path="/activities/:id/edit" element={<ActivityForm />} />

          <Route path="/reports" element={<Reports />} />
          <Route path="/search" element={<SearchResults />} />

          {/* Admin / Manager only */}
          <Route element={<ProtectedRoute roles={['Admin', 'Manager']} />}>
            <Route path="/users" element={<UserList />} />
            <Route path="/audit-logs" element={<AuditLogList />} />
          </Route>

          {/* Admin only */}
          <Route element={<ProtectedRoute roles={['Admin']} />}>
            <Route path="/users/new" element={<UserForm />} />
            <Route path="/users/:id/edit" element={<UserForm />} />
            <Route path="/roles" element={<RolesPage />} />
          </Route>

          <Route path="/forbidden" element={<Forbidden />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Route>
    </Routes>
  );
}
