async function deletePublication(publicationId) {
    const response = await fetch(server + "/publications/" + publicationId, {
        method: "DELETE",
        headers: { "Content-Type": "application/json", "Authorization": "Bearer " + sessionStorage.getItem(accessToken), "Accept-Language": currentCulture },
        body: JSON.stringify({
            username: document.getElementById("username").value,
            password: document.getElementById("password").value
        })
    });

    if (response.ok === true) {
        alert("Статья удалена")
    }
    else {
        var errorMessage = await responseError(response);
        alert(errorMessage);
    }
}

async function updatePublication(publicationId) {
    const response = await fetch(server + "/publications", {
        method: "PUT",
        headers: { "Content-Type": "application/json", "Authorization": "Bearer " + sessionStorage.getItem(accessToken), "Accept-Language": currentCulture },
        body: JSON.stringify({
            publicationId: publicationId,
            title: document.getElementById("title").value,
            content: document.getElementById("content").value
        })
    });
    if (response.ok === true) {
        alert("Статья обновлена")
    }
    else {
        var errorMessage = await responseError(response);
        alert(errorMessage);
    }
}