import { Route, Routes } from "react-router-dom";
import { CustomerPage } from "./customer/CustomerPage";
import { KitchenPage } from "./kitchen/KitchenPage";

export function App() {
  return (
    <Routes>
      <Route path="/" element={<CustomerPage />} />
      <Route path="/kitchen" element={<KitchenPage />} />
    </Routes>
  );
}

export default App;
