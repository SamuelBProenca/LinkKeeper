"use strict";
const $ = id => document.getElementById(id);
let csrf = "";
const message = text => { $("message").textContent = text; };
async function refreshCsrf() { const response = await fetch("/api/csrf", {cache:"no-store"}); if(!response.ok) throw Error("Não foi possível iniciar a sessão."); csrf = (await response.json()).token; }
async function request(path, method="GET", data) {
 const response = await fetch(path,{method,cache:"no-store",headers:{"Content-Type":"application/json","X-CSRF-TOKEN":csrf},body:data===undefined?undefined:JSON.stringify(data)});
 if(response.status===401){$("login").hidden=false;$("workspace").hidden=true;throw Error("Entre com sua senha para continuar.");}
 if(!response.ok){let result={};try{result=await response.json();}catch{}throw Error(result.error || (response.status===429?"Muitas tentativas. Aguarde um minuto.":"Não foi possível concluir. Tente novamente."));}
 return response.status===204?null:response.json();
}
async function load() {
 const params=new URLSearchParams({status:$("status").value});if($("category").value)params.set("category",$("category").value);if($("search").value.trim())params.set("q",$("search").value.trim());
 const list=await request("/api/links/?"+params);
 $("login").hidden=true;$("workspace").hidden=false;$("items").replaceChildren();
 if(!list.length){const empty=document.createElement("p");empty.className="empty";empty.textContent="Nenhum link nesta seleção. Guarde uma referência ou ajuste os filtros.";$("items").append(empty);}
 for(const item of list){
 const card=document.createElement("article");card.className="item";
 const tag=document.createElement("span");tag.className="tag";tag.textContent=item.category;
 const link=document.createElement("a");link.href=item.originalUrl;link.target="_blank";link.rel="noopener noreferrer";link.textContent=item.originalUrl;
 const description=document.createElement("p");description.textContent=item.description;
 const meta=document.createElement("div");meta.className="meta";meta.textContent=item.expiresAt?"Validade: "+new Date(item.expiresAt).toLocaleString("pt-BR"):"Sem expiração";
 card.append(tag,link,description,meta);
 const actions=document.createElement("div");actions.className="actions";
 if(item.status!=="Archived"){const button=document.createElement("button");button.className="quiet";button.textContent="Arquivar";button.onclick=()=>perform(button,async()=>{await request(`/api/links/${item.id}/archive`,"POST");await load();message("Link arquivado.");});actions.append(button);}
 if(item.status==="Expired"){const button=document.createElement("button");button.textContent="Renovar por 7 dias";button.onclick=()=>perform(button,async()=>{await request(`/api/links/${item.id}/renew`,"POST",{days:7});await load();message("Link renovado por sete dias.");});actions.append(button);}
 card.append(actions);$("items").append(card);
 }
}
async function perform(button, action){button.disabled=true;try{await action();}catch(error){message(error.message);}finally{button.disabled=false;}}
$("loginForm").onsubmit=event=>{event.preventDefault();perform(event.submitter,async()=>{await request("/api/login","POST",{password:$("password").value});$("password").value="";await refreshCsrf();await load();message("");});};
$("createForm").onsubmit=event=>{event.preventDefault();perform(event.submitter,async()=>{const result=await request("/api/links/","POST",{command:$("command").value,description:$("description").value});$("createForm").reset();$("status").value="Active";await load();message(result.created?"Link guardado.":"Este link já existe nessa categoria. A validade original foi mantida.");});};
$("refresh").onclick=event=>perform(event.currentTarget,load);
for(const id of ["status","category"])$(id).onchange=()=>load().catch(error=>message(error.message));
let searchTimer;$("search").oninput=()=>{clearTimeout(searchTimer);searchTimer=setTimeout(()=>load().catch(error=>message(error.message)),250);};
$("logout").onclick=event=>perform(event.currentTarget,async()=>{await request("/api/logout","POST");$("login").hidden=false;$("workspace").hidden=true;$("items").replaceChildren();await refreshCsrf();message("Você saiu do seu espaço.");});
refreshCsrf().then(load).catch(error=>{if(!error.message.startsWith("Entre"))message(error.message);});
