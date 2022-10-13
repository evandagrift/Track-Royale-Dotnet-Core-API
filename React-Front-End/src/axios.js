import Axios from "axios";
const localURL = "//localhost:52003/api/"
const baseURL = "https://www.royaletracker.com/api/";
let headers = {};
if (localStorage.user){
   headers.Authorization = `bearer ${localStorage.user['token']}`;
}
export const axios = Axios.create({baseURL:baseURL,
headers,
});
