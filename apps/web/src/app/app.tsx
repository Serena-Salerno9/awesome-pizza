import { Route, Routes } from "react-router-dom";
import { BoardPage } from "./board/BoardPage";
import { CustomerPage } from "./customer/CustomerPage";
import { KitchenPage } from "./kitchen/KitchenPage";
import { AppNav } from "./shared/AppNav";

export function App() {
  return (
    <>
      <AppNav />
      <Routes>
        <Route path="/" element={<CustomerPage />} />
        <Route path="/tabellone" element={<BoardPage />} />
        <Route path="/kitchen" element={<KitchenPage />} />
      </Routes>
    </>
  );
}

export default App;
